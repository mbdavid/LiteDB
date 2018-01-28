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
        private const int MAX_CACHE_SIZE = 1000;

        private ConcurrentDictionary<long, BasePage> _cache = new ConcurrentDictionary<long, BasePage>();

        private ConcurrentBag<Stream> _pool = new ConcurrentBag<Stream>();
        private IDiskFactory _factory;
        private TimeSpan _timeout;
        private long _sizeLimit;
        private AesEncryption _crypto = null;
        private Logger _log;

        private Stream _writer;

        public FileService(IDiskFactory factory, string password, TimeSpan timeout, long initialSize, long sizeLimit, Logger log)
        {
            _factory = factory;
            _timeout = timeout;
            _sizeLimit = sizeLimit;
            _log = log;

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
                _writer.Position = _writer.Length;
            }

            // lock datafile if stream are FileStream (single process)
            if (_writer.TryLock(_timeout)) throw LiteException.AlreadyOpenDatafile(factory.Filename);

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
            var header = this.ReadPage(0) as HeaderPage;

            // test hash password
            var hash = AesEncryption.HashPBKDF2(password, header.Salt);

            if (hash.BinaryCompareTo(header.Password) != 0)
            {
                throw LiteException.DatabaseWrongPassword();
            }

            _crypto = new AesEncryption(password, header.Salt);
        }

        /// <summary>
        /// Return stream length
        /// </summary>
        public long Length => _writer.Length;

        /// <summary>
        /// Read page bytes from disk (use stream pool) - Always return a fresh (never used) page instance
        /// </summary>
        public BasePage ReadPage(long position)
        {
            var stream = _pool.TryTake(out var s) ? s : _factory.GetStream();

            try
            {
                // position cursor
                stream.Position = position;

                return this.ReadPage(stream);
            }
            finally
            {
                // add stream back to pool
                _pool.Add(stream);
            }
        }

        /// <summary>
        /// Read page from current reader stream position
        /// </summary>
        private BasePage ReadPage(Stream stream)
        {
            // if page are inside local cache, return new instance of this page (avoid disk read)
            if (_cache.TryGetValue(stream.Position, out var cached))
            {
                // move stream cursor
                stream.Position += BasePage.PAGE_SIZE;
            
                // return cloned page
                return cached.Clone();
            }

            var position = stream.Position;
            var buffer = new byte[BasePage.PAGE_SIZE];

            // read bytes from data file
            stream.Read(buffer, 0, BasePage.PAGE_SIZE);

            // if datafile is encrypted and is not first header page
            var bytes = _crypto == null || stream.Position == 0 ? buffer : _crypto.Decrypt(buffer);

            // convert bytes into page
            var page = BasePage.ReadPage(bytes);

            // add this page to local cache or clear cache if reach max limit
            if (_cache.Count < MAX_CACHE_SIZE)
            {
                _cache.AddOrUpdate(position, page, (pos, pg) => page);
            }
            else
            {
                _cache.Clear();
            }

            return page;
        }

        /// <summary>
        /// Get/Set position of writer stream
        /// </summary>
        public long WriterPosition { get => _writer.Position; set => _writer.Position = value; }

        /// <summary>
        /// Write all pages into datafile using current writer position. Fill pagePositions for each page saved
        /// </summary>
        public void WritePages(IEnumerable<BasePage> pages, bool absolute, IDictionary<uint, PagePosition> pagePositions)
        {
            lock (_writer)
            {
                foreach (var page in pages)
                {
                    // serialize page
                    var buffer = page.WritePage();

                    // get position before write on disk
                    var position = _writer.Position;

                    // encrypt if not header page (exclusive on position 0)
                    var bytes = _crypto == null || position == 0 ? buffer : _crypto.Encrypt(buffer);

                    // if absolute position, set cursor position to pageID (otherwise use current position increment)
                    if (absolute)
                    {
                        _writer.Position = BasePage.GetPagePosition(page.PageID);
                    }

                    if (position > _sizeLimit) throw LiteException.FileSizeExceeded(_sizeLimit);

                    // write on disk
                    _writer.Write(bytes, 0, BasePage.PAGE_SIZE);

                    // add this page to cache too (mark as clean page)
                    page.IsDirty = false;

                    // add this page to local cache or clear cache if reach max limit
                    if (_cache.Count < MAX_CACHE_SIZE)
                    {
                        _cache.AddOrUpdate(position, page, (pos, pg) => page);
                    }
                    else
                    {
                        _cache.Clear();
                    }

                    if (pagePositions != null)
                    {
                        pagePositions[page.PageID] = new PagePosition(page.PageID, position);
                    }
                }
            }
        }

        /// <summary>
        /// Create new database based if Stream are empty
        /// </summary>
        public void CreateDatabase(string password, long initialSize)
        {
            // create a new header page in bytes (fixed in 0)
            var header = new HeaderPage
            {
                Salt = AesEncryption.Salt()
            };

            // hashing password using PBKDF2
            if (password != null)
            {
                header.Password = AesEncryption.HashPBKDF2(password, header.Salt);
            }

            // create collection list page (fixed in 1)
            var colList = new CollectionListPage();

            // create empty page just for lock control (fixed in 2)
            var locker = new EmptyPage(2);

            // write all pages into disk
            this.WritePages(new BasePage[] { header, colList, locker }, false, null);

            // if has initial size (at least 10 pages), alocate disk space now
            if (initialSize > (BasePage.PAGE_SIZE * 10))
            {
                _writer.SetLength(initialSize);
            }
        }

        /// <summary>
        /// Dispose all stream in pool
        /// </summary>
        public void Dispose()
        {
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