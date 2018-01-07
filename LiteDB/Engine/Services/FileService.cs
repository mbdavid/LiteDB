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
        private Logger _log;
        private AesEncryption _crypto = null;
        private long _position = 0; // get writer position (for sequencial writes)

        private Task _asyncWriter = null;
        private ConcurrentQueue<Tuple<long, byte[]>> _queueWriter = new ConcurrentQueue<Tuple<long, byte[]>>();

        public FileService(IDiskFactory factory, TimeSpan timeout, long sizeLimit, Logger log)
        {
            _factory = factory;
            _timeout = timeout;
            _log = log;
            _sizeLimit = sizeLimit;
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
        /// Check if stream are empty
        /// </summary>
        public bool IsEmpty()
        {
            // if file do not exists, return true without getting stream (get stream creates files)
            if (_factory.Exists() == false) return true;

            // now, get stream to test strem length
            var stream = _pool.TryTake(out var s) ? s : _factory.GetStream();

            try
            {
                return stream.Length == 0;
            }
            finally
            {
                // add stream back to pool
                _pool.Add(stream);
            }
        }

        /// <summary>
        /// Return file size
        /// </summary>
        public long FileSize()
        {
            var stream = _pool.TryTake(out var s) ? s : _factory.GetStream();

            try
            {
                return stream.Length;
            }
            finally
            {
                // add stream back to pool
                _pool.Add(stream);
            }
        }

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
        /// Read all pages from stream starting in position 0
        /// </summary>
        public IEnumerable<BasePage> ReadAllPages()
        {
            var stream = _pool.TryTake(out var s) ? s : _factory.GetStream();

            try
            {
                // read all pages from initial 0 position
                stream.Position = 0;

                while(stream.Position < stream.Length)
                {
                    yield return this.ReadPage(stream);
                }
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
        /// Write all pages bytes into disk using stream from pool (page position according pageID). Return an IEnumerable, so need execute ToArray()
        /// </summary>
        public IList<PagePosition> WritePages(IEnumerable<BasePage> pages)
        {
            var stream = _pool.TryTake(out var s) ? s : _factory.GetStream();

            try
            {
                var result = new List<PagePosition>();

                foreach (var page in pages)
                {
                    // position stream based on pageID
                    stream.Position = BasePage.GetPagePostion(page.PageID);

                    result.Add(this.WritePage(stream, page));
                }

                return result;
            }
            finally
            {
                // add stream back to pool
                _pool.Add(stream);
            }
        }

        /// <summary>
        /// Write all pages bytes into disk using single stream (sequencial write)
        /// </summary>
        public void WritePagesSequence(IEnumerable<BasePage> pages, IDictionary<uint, PagePosition> pagePositions = null)
        {
            var stream = _pool.TryTake(out var s) ? s : _factory.GetStream();

            try
            {
                foreach (var page in pages)
                {
                    // locked get/update _position writer cursor - increase by PAGE_SIZE
                    lock (_pool)
                    {
                        stream.Position = _position;

                        _position += BasePage.PAGE_SIZE;
                    }

                    var pos = this.WritePage(stream, page);

                    // if dictionary as passed, inserted position
                    if (pagePositions != null)
                    {
                        pagePositions[page.PageID] = pos;
                    }
                }
            }
            finally
            {
                // add stream back to pool
                _pool.Add(stream);
            }
        }

        /// <summary>
        /// Write a single page into curret Stream position
        /// </summary>
        private PagePosition WritePage(Stream stream, BasePage page)
        {
            // serialize page
            var buffer = page.WritePage();

            // encrypt if not header page
            var bytes = _crypto == null || page.PageID == 0 ? buffer : _crypto.Encrypt(buffer);

            // get position before write on disk
            var position = stream.Position;

            if (position > _sizeLimit) throw LiteException.FileSizeExceeded(_sizeLimit);

            stream.Write(bytes, 0, BasePage.PAGE_SIZE);

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

            return new PagePosition(page.PageID, position);
        }

        /// <summary>
        /// Clear all file content and position cursor to initial file. Do not delete file from disk (only content)
        /// </summary>
        public void Clear()
        {
            lock (_cache)
            {
                _cache.Clear();

                // create single instance of Stream writer if not exists yet
                var stream = _pool.TryTake(out var s) ? s : _factory.GetStream();

                try
                {
                    _position = 0;
                    stream.SetLength(0);
                }
                finally
                {
                    // add stream back to pool
                    _pool.Add(stream);
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
            var colList = new CollectionListPage()
            {
            };

            this.WritePages(new BasePage[] { header, colList }).Execute();

            // if has initial size (at least 10 pages), alocate disk space now
            if (initialSize > (BasePage.PAGE_SIZE * 10))
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Dispose all stream in pool
        /// </summary>
        public void Dispose()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Dispose all stream in pool (with delete file option)
        /// </summary>
        public void Dispose(bool delete)
        {
            // delete file only if delete=true AND file content is empty (length = 0)
            var empty = delete ? this.IsEmpty() : false;

            if (_crypto != null) _crypto.Dispose();

            if (_factory.Dispose)
            {
                while (_pool.TryTake(out var stream))
                {
                    stream.Dispose();
                }
            }

            if (empty)
            {
                _factory.Delete();
            }
        }
    }
}