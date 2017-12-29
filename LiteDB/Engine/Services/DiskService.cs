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
        private ConcurrentBag<Stream> _pool = new ConcurrentBag<Stream>();
        private IDiskFactory _factory;
        private AesEncryption _crypto;
        private TimeSpan _timeout;
        private Stream _writer;

        public FileService(IDiskFactory factory, string password, TimeSpan timeout)
        {
            _factory = factory;
            _timeout = timeout;

            var stream = _pool.TryTake(out var s) ? s : _factory.GetStream();
        }

        /// <summary>
        /// Read page bytes from disk
        /// </summary>
        public byte[] ReadPage(long position)
        {
            var stream = _pool.TryTake(out var s) ? s : _factory.GetStream();

            try
            {
                var buffer = new byte[BasePage.PAGE_SIZE];

                // position cursor
                stream.Position = position;

                // read bytes from data file
                stream.Read(buffer, 0, BasePage.PAGE_SIZE);

                // decrypt here!!!!

                return buffer;
            }
            finally
            {
                // add stream back to pool
                _pool.Add(stream);
            }
        }

        /// <summary>
        /// Persist single page bytes to disk
        /// </summary>
        public void WritePage(IEnumerable<BasePage> pages, bool sequencial)
        {
            // create single instance of Stream writer
            if (_writer == null)
            {
                _writer = _factory.GetStream();
            }


            var position = BasePage.GetSizeOfPages(pageID);

            // this lock is only for precaution
            lock (stream)
            {
                // position cursor
                stream.Position = position;

                stream.Write(buffer, 0, BasePage.PAGE_SIZE);

                return stream.Position;
            }
        }

        /// <summary>
        /// Create new database based if Stream are empty
        /// </summary>
        public void CreateDatabase(long initialSize)
        {


            // create database only if not exists
            if (stream.Length == 0) return;

            // create a new header page in bytes (keep second page empty)
            var header = new HeaderPage
            {
                LastPageID = 1,
                Salt = AesEncryption.Salt()
            };

            // point to begin file
            stream.Seek(0, SeekOrigin.Begin);

            // get header page in bytes
            var buffer = header.WritePage();

            stream.Write(buffer, 0, BasePage.PAGE_SIZE);

            // if has initial size (at least 10 pages), alocate disk space now
            if (initialSize > (BasePage.PAGE_SIZE * 10))
            {
                stream.SetLength(initialSize);
            }
        }

        /// <summary>
        /// Dispose all stream in pool
        /// </summary>
        public void Dispose()
        {
            if (!_factory.Dispose) return;

            if (_writer != null) _writer.Dispose();

            while (_pool.TryTake(out var stream))
            {
                stream.Dispose();
            }
        }
    }
}