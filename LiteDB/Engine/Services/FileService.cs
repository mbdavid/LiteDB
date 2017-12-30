using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
        private Stream _writer;

        public FileService(IDiskFactory factory, TimeSpan timeout, long sizeLimit)
        {
            _factory = factory;
            _timeout = timeout;

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
            // get first stream and add into pool
            var reader = _factory.GetStream();

            _pool.Add(reader);

            return reader.Length == 0;
        }

        /// <summary>
        /// Read page bytes from disk (use stream pool)
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
            var buffer = new byte[BasePage.PAGE_SIZE];

            // read bytes from data file
            stream.Read(buffer, 0, BasePage.PAGE_SIZE);

            // if datafile is encrypted and is not first header page
            var bytes = _crypto == null || stream.Position == 0 ? buffer : _crypto.Decrypt(buffer);

            // convert bytes into page
            var page = BasePage.ReadPage(bytes);

            return page;
        }

        /// <summary>
        /// Write all pages bytes into disk using stream from pool (page position according pageID)
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

            stream.Write(bytes, 0, BasePage.PAGE_SIZE);

            return new PagePosition(page.PageID, stream.Position);
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
                LastPageID = 1,
                Salt = AesEncryption.Salt()
            };

            this.WritePages(new BasePage[] { header });

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
            if (_crypto != null) _crypto.Dispose();

            if (!_factory.Dispose) return;

            if (_writer != null) _writer.Dispose();

            while (_pool.TryTake(out var stream))
            {
                stream.Dispose();
            }
        }
    }
}