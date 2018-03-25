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
        /// <summary>
        /// Header info the validate that datafile is a LiteDB file [27 bytes]
        /// </summary>
        private const string HEADER_INFO = "** This is a LiteDB file **";

        /// <summary>
        /// Datafile specification version [1 byte]
        /// </summary>
        private const byte FILE_VERSION = 8;

        private ConcurrentBag<BinaryReader> _pool = new ConcurrentBag<BinaryReader>();
        private Func<BinaryReader> _readerFactory;
        private IDiskFactory _factory;

        private TimeSpan _timeout;
        private long _sizeLimit;
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

            // get first stream (will be used as single writer)
            var stream = factory.GetStream();

            try
            {
                // if empty datafile, create database here
                if (stream.Length == 0)
                {
                    this.CreateDatafile(stream, password, initialSize);
                }
                else
                {
                    // otherwise, read header page to 
                    this.InitializeDatafile(stream, password);
                }

                // update virtual file length with real file length
                _virtualPosition = stream.Length;
                _virtualLength = stream.Length;

                //TODO: lock datafile if stream are FileStream (single process)
                // if (stream.TryLock(_timeout) == false) throw LiteException.AlreadyOpenDatafile(factory.Filename);
            }
            catch
            {
                // close stream if any error occurs
                stream.Dispose();
                throw;
            }
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

            var reader = _readerFactory();

            try
            {
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
                _pool.Add(reader);
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
            _writer.BaseStream.Flush();
        }

        /// <summary>
        /// Create new datafile based in empty Stream
        /// </summary>
        private void CreateDatafile(Stream stream, string password, long initialSize)
        {
            // create 16 bytes salt to store on end of header page
            var salt = password == null ? new byte[16] : AesStream.Salt();
            var hash = password == null ? new byte[20] : AesStream.HashPBKDF2(password, salt);

            var writer = new BinaryWriter(stream);

            // start writing fixed 0 area - plain writer, no encription
            // use fixed 64 bytes for this (same area size)
            writer.WriteFixedString(HEADER_INFO);
            writer.Write(FILE_VERSION);
            writer.Write(hash);
            writer.Write(salt);

            // initialize _writer/reader
            this.InitializeReaderWriter(stream, password, salt);

            // create a new header page and write on disk (sync)
            var header = new HeaderPage(0);

            header.WritePage(_writer);

            // if has initial size (at least 10 pages), alocate disk space now
            if (initialSize > (BasePage.PAGE_SIZE * 10))
            {
                //TODO must implement linked list - this initial will shrink in first checkpoint
                _writer.BaseStream.SetLength(initialSize);
            }
        }

        /// <summary>
        /// Read initial header data from header area in header page (info/version/password/salt)
        /// </summary>
        private void InitializeDatafile(Stream stream, string password)
        {
            var reader = new BinaryReader(stream);

            var info = reader.ReadFixedString(HEADER_INFO.Length);
            var version = reader.ReadByte();

            if (info != HEADER_INFO) throw LiteException.InvalidDatabase();
            if (version != FILE_VERSION) throw LiteException.InvalidDatabaseVersion(version);

            var hash = reader.ReadBytes(20);
            var salt = reader.ReadBytes(16);

            // if hash is not empty but password are empty, throw missing password exception
            if (hash.Any(b => b != 0) && password == null) throw LiteException.DatabaseWrongPassword();

            // checks if password match
            if (password != null)
            {
                var pass = AesStream.HashPBKDF2(password, salt);

                if (hash.BinaryCompareTo(pass) != 0) throw LiteException.DatabaseWrongPassword();
            }

            // initialize writer/readerFactory
            this.InitializeReaderWriter(stream, password, salt);
        }

        /// <summary>
        /// Initialize _writer and _readerFactory
        /// </summary>
        private void InitializeReaderWriter(Stream stream, string password, byte[] salt)
        {
            // clear stream position
            stream.Position = 0;

            _writer = new BinaryWriter(password == null ? stream : new AesStream(stream, password, salt));

            // initialize reader factory
            _readerFactory = () =>
            {
                if (_pool.TryTake(out var r)) return r;

                var st = _factory.GetStream();

                return new BinaryReader(password == null ? st : new AesStream(st, password, salt));
            };
        }

        /// <summary>
        /// Dispose all stream in pool and async writer
        /// </summary>
        public void Dispose()
        {
            // wait async
            this.WaitAsyncWrite();

            if (_factory.CloseOnDispose)
            {
                // first dispose writer
                _writer.BaseStream.Dispose();

                // after, dispose all readers
                while (_pool.TryTake(out var stream))
                {
                    stream.Dispose();
                }
            }
        }
    }
}