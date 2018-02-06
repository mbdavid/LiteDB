// async writer
// falta controle da cache de paginas sujas vs limpas
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB
{
    /// <summary>
    /// Implement datafile read/write operation with encryption and stream pool
    /// </summary>
    internal class FileService : IDisposable
    {
        private ConcurrentBag<Stream> _pool = new ConcurrentBag<Stream>();
        private IDiskFactory _factory;
        private TimeSpan _timeout;
        private long _sizeLimit;
        private AesEncryption _crypto = null;
        private Logger _log;
        private CacheService _cache;

        private Stream _writer;
        private long _writerPosition = 0;

        // async writer control
        private ConcurrentQueue<Tuple<long, BasePage>> _queue = new ConcurrentQueue<Tuple<long, BasePage>>();
        private Task _async;

        public FileService(IDiskFactory factory, string password, TimeSpan timeout, long initialSize, long sizeLimit, Logger log)
        {
            _factory = factory;
            _timeout = timeout;
            _sizeLimit = sizeLimit;
            _log = log;

            // initialize cache service
            _cache = new CacheService(_log);

            // create writer instance (single writer)
            _writer = factory.GetStream();

            // if stream are empty, create inital database
            if (_writer.Length == 0)
            {
                this.CreateDatabase(password, initialSize);
            }
            else
            {
                // if file exits, position at end (to append wal data)
                _writerPosition = _writer.Length;
            }

            // lock datafile if stream are FileStream (single process)
            if (_writer.TryLock(_timeout) == false) throw LiteException.AlreadyOpenDatafile(factory.Filename);

            // enable encryption
            if (password != null)
            {
                this.EnableEncryption(password);
            }
        }

        /// <summary>
        /// Load AES library and encrypt all pages before write on disk (except Header Page - 0). Must run before start using class
        /// </summary>
        public void EnableEncryption(string password)
        {
            // read header from disk in page 0
            var header = this.ReadPage(0, false) as HeaderPage;

            // test hash password
            var hash = AesEncryption.HashPBKDF2(password, header.Salt);

            if (hash.BinaryCompareTo(header.Password) != 0)
            {
                throw LiteException.DatabaseWrongPassword();
            }

            _crypto = new AesEncryption(password, header.Salt);
        }

        /// <summary>
        /// Get/Set stream length
        /// </summary>
        public long Length { get => _writer.Length; set => _writer.SetLength(value); }

        /// <summary>
        /// Read page bytes from disk (use stream pool) - Always return a fresh (never used) page instance.
        /// </summary>
        public BasePage ReadPage(long position, bool clone)
        {
            // try get page from cache
            var page = _cache.GetPage(position, clone);

            if (page != null) return page;

            var stream = _pool.TryTake(out var s) ? s : _factory.GetStream();

            try
            {
                // position cursor
                stream.Position = position;

                var buffer = new byte[BasePage.PAGE_SIZE];

                // read bytes from data file
                stream.Read(buffer, 0, BasePage.PAGE_SIZE);

                // if datafile is encrypted and is not first header page
                var bytes = _crypto == null || stream.Position == 0 ? buffer : _crypto.Decrypt(buffer);

                // convert bytes into page
                page = BasePage.ReadPage(bytes);

                // add fresh disk page into cache
                _cache.AddPage(position, page);

                return page;
            }
            finally
            {
                // add stream back to pool
                _pool.Add(stream);
            }
        }

        /// <summary>
        /// Get/Set position of writer stream
        /// </summary>
        public long WriterPosition { get => _writerPosition; set => _writerPosition = value; }

        /// <summary>
        /// Write all pages into datafile using current writer position. Fill pagePositions for each page saved
        /// </summary>
        public void WritePages(IEnumerable<BasePage> pages, bool absolute, IDictionary<uint, PagePosition> pagePositions)
        {
            // lock writer but don't use writer here (will be used only in async writer task)
            lock (_writer)
            {
                foreach (var page in pages)
                {
                    // mark sure that page are marked as dirty (will be clean on async write)
                    page.IsDirty = true;

                    // if absolute position, set cursor position to pageID (otherwise use current position increment)
                    if (absolute)
                    {
                        _writerPosition = BasePage.GetPagePosition(page.PageID);
                    }

                    // test max file size (includes wal operations)
                    if (_writerPosition > _sizeLimit) throw LiteException.FileSizeExceeded(_sizeLimit);

                    // add dirty page to cache
                    _cache.AddPage(_writerPosition, page);

                    // add to writer queue
                    _queue.Enqueue(new Tuple<long, BasePage>(_writerPosition, page));

                    // return page position on disk (where will be write on disk)
                    if (pagePositions != null)
                    {
                        pagePositions[page.PageID] = new PagePosition(page.PageID, _writerPosition);
                    }

                    _writerPosition += BasePage.PAGE_SIZE;
                }

                // if async writer are not running, start now
                if (_async == null || _async.Status == TaskStatus.RanToCompletion)
                {
                    _async = this.CreateAsyncWriter();
                    _async.Start();
                }
            }
        }

        /// <summary>
        /// Implement async writer disk in a background task
        /// </summary>
        private Task CreateAsyncWriter()
        {
            return new Task(() =>
            {
                // write all pages that are in queue
                while (!_queue.IsEmpty)
                {
                    // get page from queue
                    if (!_queue.TryDequeue(out var item)) break;

                    var position = item.Item1;
                    var page = item.Item2;

                    var buffer = page.WritePage();

                    // encrypt if not header page (exclusive on position 0)
                    var bytes = _crypto == null || position == 0 ? buffer : _crypto.Encrypt(buffer);

                    _writer.Position = position;

                    _writer.Write(bytes, 0, BasePage.PAGE_SIZE);
                }

                // lock writer to clear dirty cache
                lock(_writer)
                {
                    // before clear cache, test if queue are empty, otherwise do not clear cache.
                    if (_queue.IsEmpty)
                    {
                        _cache.ClearDirtyCache();
                    }
                }

            });
        }

        /// <summary>
        /// Create new database based if Stream are empty
        /// </summary>
        public void CreateDatabase(string password, long initialSize)
        {
            // create a new header page in bytes (fixed in 0)
            var header = new HeaderPage(0)
            {
                Salt = AesEncryption.Salt(),
                LastPageID = 2
            };

            // hashing password using PBKDF2
            if (password != null)
            {
                header.Password = AesEncryption.HashPBKDF2(password, header.Salt);
            }

            // create empty page just for lock control (fixed in 1)
            var locker = new EmptyPage(1);

            // write all pages into disk
            this.WritePages(new BasePage[] { header, locker }, false, null);

            // if has initial size (at least 10 pages), alocate disk space now
            if (initialSize > (BasePage.PAGE_SIZE * 10))
            {
                _writer.SetLength(initialSize);
            }
        }

        /// <summary>
        /// Dispose all stream in pool and async writer
        /// </summary>
        public void Dispose()
        {
            // if has pages on queue but async writer are not running, run sync
            if (_queue.IsEmpty == false && _async.Status == TaskStatus.RanToCompletion)
            {
                this.CreateAsyncWriter().RunSynchronously();
            }

            // if async writer are running, wait to finish
            if (_async != null && _async.Status != TaskStatus.RanToCompletion)
            {
                _async.Wait();
            }

            // dispose crypto
            if (_crypto != null)
            {
                _crypto.Dispose();
            }

            if (_factory.CloseOnDispose)
            {
                // first dispose writer
                _writer.TryUnlock();
                _writer.Dispose();

                // after, dispose all readers
                while (_pool.TryTake(out var stream))
                {
                    stream.Dispose();
                }
            }
        }
    }
}