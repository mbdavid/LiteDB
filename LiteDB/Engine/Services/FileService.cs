using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Implement datafile read/write operation with encryption and stream pool
    /// </summary>
    internal class FileService : IDisposable
    {
        private ConcurrentDictionary<long, BasePage> _cache = new ConcurrentDictionary<long, BasePage>();

        private ConcurrentBag<Stream> _pool = new ConcurrentBag<Stream>();
        private IDiskFactory _factory;
        private TimeSpan _timeout;
        private AesEncryption _crypto = null;
        private long _sizeLimit;
        private Logger _log;
        private Stream _writer;

        public FileService(IDiskFactory factory, TimeSpan timeout, long sizeLimit, Logger log)
        {
            _factory = factory;
            _timeout = timeout;
            _log = log;
            _sizeLimit = sizeLimit;

            _writer = factory.GetStream();
        }

        /// <summary>
        /// Load AES library and encrypt all pages before write on disk (except Header Page - 0). Must run before start using class
        /// </summary>
        public void EnableEncryption(string password)
        {
            // if there is no Salt loaded, read from header page (0)
            var salt = (this.ReadPage(0) as HeaderPage).Salt;

            _crypto = new AesEncryption(password, salt);
        }

        /// <summary>
        /// Check if stream are empty
        /// </summary>
        public bool IsEmpty()
        {
            var stream = _pool.TryTake(out var s) ? s : _factory.GetStream();

            _pool.Add(stream);

            return stream.Length == 0;
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
#if DEBUG
                _log.Write(Logger.DISK, "'{0}' read page cache: id {1} ({2}) on position {3}", Path.GetFileName(_factory.Filename), cached.PageID == uint.MaxValue ? "-" : cached.PageID.ToString(), cached.PageType, stream.Position);
#endif

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

            // add this page to local cache
            _cache.AddOrUpdate(position, page, (pos, pg) => page);

#if DEBUG
            _log.Write(Logger.DISK, "'{0}' read page disk: id {1} ({2}) on position {3}", Path.GetFileName(_factory.Filename), page.PageID == uint.MaxValue ? "-" : page.PageID.ToString(), page.PageType, position);
#endif

            return page;
        }

        /// <summary>
        /// Write all pages bytes into disk using stream from pool (page position according pageID). Return an IEnumerable, so need execute ToArray()
        /// </summary>
        public IEnumerable<PagePosition> WritePages(IEnumerable<BasePage> pages)
        {
            var stream = _pool.TryTake(out var s) ? s : _factory.GetStream();

            try
            {
                foreach (var page in pages)
                {
                    // position stream based on pageID
                    stream.Position = BasePage.GetPagePostion(page.PageID);

                    yield return this.WritePage(stream, page);
                }
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
        public IEnumerable<PagePosition> WritePagesSequencial(IEnumerable<BasePage> pages)
        {
            // write operation occurs in single process (can run in async queue task)
            lock(_writer)
            {
                foreach (var page in pages)
                {
                    yield return this.WritePage(_writer, page);
                }
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

#if DEBUG
            _log.Write(Logger.DISK, "'{0}' write page disk: id {1} ({2}) on position {3} transaction '{4}'", Path.GetFileName(_factory.Filename), page.PageID == uint.MaxValue ? "-" : page.PageID.ToString(), page.PageType, position, page.TransactionID.ToString().Substring(0, 4));
#endif

            stream.Write(bytes, 0, BasePage.PAGE_SIZE);

            // add this page to cache too (mark as clean page)
            page.IsDirty = false;

            _cache.AddOrUpdate(position, page, (pos, pg) => page);

            return new PagePosition(page.PageID, position);
        }

        /// <summary>
        /// Clear all file content and position cursor to initial file. Do not delete file from disk
        /// </summary>
        public void Clear()
        {
            _cache.Clear();

            // create single instance of Stream writer if not exists yet
            if (_writer == null) _writer = _factory.GetStream();

            _writer.SetLength(0);
            _writer.Position = 0;
        }

        /// <summary>
        /// Create new database based if Stream are empty
        /// </summary>
        public void CreateDatabase(long initialSize)
        {
            // create a new header page in bytes (keep second page empty)
            var header = new HeaderPage
            {
                Salt = AesEncryption.Salt()
            };

            this.WritePages(new BasePage[] { header }).Execute();

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

            if (!_factory.Dispose) return;

            if (_writer != null) _writer.Dispose();

            while (_pool.TryTake(out var stream))
            {
                stream.Dispose();
            }

            if (empty)
            {
                _factory.Delete();
            }
        }
    }
}