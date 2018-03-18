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
    /// Implement thread safe stream data access
    /// - Pages can be encrypted in write and decrypted on read (except header page)
    /// - Pages are stored in memory cache
    /// - Read operations use pool of streams - multiple reads
    /// - Single writer operation that use queue to run in async task
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
        private bool _utcDate;

        private BinaryWriter _writer;
        private long _virtualPosition = 0;
        private long _virtualLength = 0;

        // async writer control
        private ConcurrentQueue<Tuple<long, BasePage>> _queue = new ConcurrentQueue<Tuple<long, BasePage>>();
        private Task _async;

        public FileService(IDiskFactory factory, string password, TimeSpan timeout, long initialSize, long sizeLimit, bool utcDate, Logger log)
        {
            _factory = factory;
            _timeout = timeout;
            _sizeLimit = sizeLimit;
            _utcDate = utcDate;
            _log = log;

            // initialize cache service
            _cache = new CacheService(_log);

            // create writer instance (single writer)
            _writer = new BinaryWriter(factory.GetStream());

            // if stream are empty, create inital database (sync)
            if (_writer.BaseStream.Length == 0)
            {
                this.CreateDatabase(password, initialSize);
            }

            // update virtual file length with real file length
            _virtualPosition = _writer.BaseStream.Length;
            _virtualLength = _writer.BaseStream.Length;

            // lock datafile if stream are FileStream (single process)
            if (_writer.BaseStream.TryLock(_timeout) == false) throw LiteException.AlreadyOpenDatafile(factory.Filename);

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
        /// Get/Set stream length - set operation must be sync before
        /// </summary>
        public long Length { get => _virtualLength; }

        /// <summary>
        /// Set new length for datafile in async mode - will be executed in queue order
        /// </summary>
        public void SetLength(long length)
        {
            lock (_writer)
            {
                // this queue item will be executed in queue async writer
                // will be run as a SetLength method on stream
                _queue.Enqueue(new Tuple<long, BasePage>(length, null));

                // update virtual file length
                _virtualLength = length;
            }
        }

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
                // if datafile is encrypted and is not first header page
                //TODO implementar novamente a encryption
                // var bytes = _crypto == null || stream.Position == 0 ? buffer : _crypto.Decrypt(buffer);
                var reader = new BinaryReader(stream);

                reader.BaseStream.Position = position;

                // read binary data and create page instance page
                page = BasePage.ReadPage(reader, _utcDate);

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
        /// Get/Set position of virtual writer stream (lock with _writer)
        /// </summary>
        public long VirtualPosition
        {
            get
            {
                lock (_writer)
                {
                    return _virtualPosition;
                }
            }
            set
            {
                lock(_writer)
                {
                    _virtualPosition = value;
                }
            }
        }

        /// <summary>
        /// Add all pages to queue using virtual position. Pages in this queue will be write on disk in async task
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
                        _virtualPosition = BasePage.GetPagePosition(page.PageID);
                    }

                    // test max file size (includes wal operations)
                    if (_virtualPosition > _sizeLimit) throw LiteException.FileSizeExceeded(_sizeLimit);

                    // add dirty page to cache
                    _cache.AddPage(_virtualPosition, page);

                    // add to writer queue
                    _queue.Enqueue(new Tuple<long, BasePage>(_virtualPosition, page));

                    // return page position on disk (where will be write on disk)
                    if (pagePositions != null)
                    {
                        pagePositions[page.PageID] = new PagePosition(page.PageID, _virtualPosition);
                    }

                    _virtualPosition += BasePage.PAGE_SIZE;

                    // update "virtual" file size
                    if (_virtualPosition > _virtualLength) _virtualLength = _virtualPosition;
                }

                // if async writer are not running, start/re-start now
                if (_async == null || _async.Status == TaskStatus.RanToCompletion)
                {
                    _async = this.CreateAsyncWriter();
                    _async.Start();
                }
            }
        }

        /// <summary>
        /// Implement async writer disk in a background task - will consume all items on queue
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

                    // if page is empty, this is special queue item: SetLength
                    if (page == null)
                    {
                        // use position as file length
                        _writer.BaseStream.SetLength(position);
                        continue;
                    }

                    _writer.BaseStream.Position = position;

                    // encrypt if not header page (exclusive on position 0)
                    //var bytes = _crypto == null || position == 0 ? buffer : _crypto.Encrypt(buffer);

                    //TODO for debug propose
                    if (page.TransactionID == Guid.Empty && BasePage.GetPagePosition(page.PageID) != position) throw new Exception("Não pode ter pagina na WAL sem transação");

                    page.WritePage(_writer);
                }

                // lock writer to clear dirty cache
                lock(_writer)
                {
                    // before clear cache, test if queue are empty, otherwise do not clear cache.
                    if (_queue.IsEmpty)
                    {
                        _cache.ClearDirty();
                    }
                }
            });
        }

        /// <summary>
        /// Lock writer and wait all queue be write on disk
        /// </summary>
        public void WaitAsyncWrite()
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

            // do a disk flush
            //TODO: can I here do a full disk flush (true) ?
            _writer.Flush();
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
                LastPageID = 1 // 0 - Header, 1 - Locker
            };

            // hashing password using PBKDF2
            if (password != null)
            {
                header.Password = AesEncryption.HashPBKDF2(password, header.Salt);
            }

            // create empty page just for lock control (fixed in 1)
            var locker = new EmptyPage(1);

            // write initial database pages (sync writes)
            header.WritePage(_writer);
            locker.WritePage(_writer);

            // if has initial size (at least 10 pages), alocate disk space now
            if (initialSize > (BasePage.PAGE_SIZE * 10))
            {
                //TODO must implement linked list - this initial will shrink in first checkpoint
                _writer.BaseStream.SetLength(initialSize);
            }
        }

        /// <summary>
        /// Dispose all stream in pool and async writer
        /// </summary>
        public void Dispose()
        {
            // wait async
            this.WaitAsyncWrite();

            // dispose crypto
            if (_crypto != null)
            {
                _crypto.Dispose();
            }

            if (_factory.CloseOnDispose)
            {
                // first dispose writer
                _writer.BaseStream.TryUnlock();
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