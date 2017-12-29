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
        private Stream _writer = null;

        public FileService(IDiskFactory factory, TimeSpan timeout, long sizeLimit)
        {
            _factory = factory;
            _timeout = timeout;
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
                var buffer = new byte[BasePage.PAGE_SIZE];

                // position cursor
                stream.Position = position;

                // read bytes from data file
                stream.Read(buffer, 0, BasePage.PAGE_SIZE);

                // if datafile is encrypted and is not first header page
                var bytes = _crypto == null || position == 0 ? buffer : _crypto.Decrypt(buffer);

                // convert bytes into page
                var page = BasePage.ReadPage(bytes);

                return page;
            }
            finally
            {
                // add stream back to pool
                _pool.Add(stream);
            }
        }

        /// <summary>
        /// Persist single page in disk
        /// </summary>
        public PagePosition WritePage(BasePage page)
        {
            // create single instance of Stream writer
            if (_writer == null) _writer = _factory.GetStream();

            // position cursor according pageID
            _writer.Position = BasePage.GetSizeOfPages(page.PageID);

            // serialize page
            var buffer = page.WritePage();

            // if file is encrypted, encrypt bytes (if not header page)
            var bytes = _crypto == null || page.PageID == 0 ? buffer : _crypto.Encrypt(buffer);

            _writer.Write(bytes, 0, BasePage.PAGE_SIZE);

            return new PagePosition(page.PageID, _writer.Position);
        }

        /// <summary>
        /// Persist all pages bytes to disk in sequece order (use single stream writer)
        /// </summary>
        public IEnumerable<PagePosition> WritePages(IEnumerable<BasePage> pages, long position = -1)
        {
            // create single instance of Stream writer
            if (_writer == null) _writer = _factory.GetStream();

            if (position > -1) _writer.Position = position;

            foreach (var page in pages)
            {
                // serialize page
                var buffer = page.WritePage();

                // encrypt if not header page
                var bytes = _crypto == null || page.PageID == 0 ? buffer : _crypto.Encrypt(buffer);

                _writer.Write(bytes, 0, BasePage.PAGE_SIZE);

                yield return new PagePosition(page.PageID, _writer.Position);
            }
        }

        /// <summary>
        /// Create new database based if Stream are empty
        /// </summary>
        public void CreateDatabase(long initialSize)
        {
            // create new Salt for AES encryption
            _salt = AesEncryption.Salt();

            // create a new header page in bytes (keep second page empty)
            var header = new HeaderPage
            {
                LastPageID = 1,
                Salt = _salt
            };

            this.WritePage(header);

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