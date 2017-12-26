using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace LiteDB
{
    internal class DiskService : IDisposable
    {
        /// <summary>
        /// Position, on page, about page type
        /// </summary>
        private const int PAGE_TYPE_POSITION = 4;

        private Stream _stream;
        private Logger _log;
        private bool _dispose;

        public DiskService(Stream stream, long initialSize, bool dispose, Logger log)
        {
            _stream = stream;
            _log = log;
            _dispose = dispose;

            if (_stream.Length == 0)
            {
                this.CreateDatabase(initialSize);
            }
        }

        /// <summary>
        /// Read page bytes from disk
        /// </summary>
        public byte[] ReadPage(uint pageID)
        {
            var buffer = new byte[BasePage.PAGE_SIZE];
            var position = BasePage.GetSizeOfPages(pageID);

            lock (_stream)
            {
                // position cursor
                _stream.Position = position;

                // read bytes from data file
                _stream.Read(buffer, 0, BasePage.PAGE_SIZE);
            }

            _log.Write(Logger.DISK, "read page #{0:0000} :: {1}", pageID, (PageType)buffer[PAGE_TYPE_POSITION]);

            return buffer;
        }

        /// <summary>
        /// Persist single page bytes to disk
        /// </summary>
        public void WritePage(uint pageID, byte[] buffer)
        {
            var position = BasePage.GetSizeOfPages(pageID);

            _log.Write(Logger.DISK, "write page #{0:0000} :: {1}", pageID, (PageType)buffer[PAGE_TYPE_POSITION]);

            lock (_stream)
            {
                // position cursor
                _stream.Position = position;

                _stream.Write(buffer, 0, BasePage.PAGE_SIZE);
            }
        }

        public long FileLength => _stream.Length;

        /// <summary>
        /// Set datafile length
        /// </summary>
        public void SetLength(long fileSize)
        {
            // fileSize parameter tell me final size of data file - helpful to extend first datafile
            _stream.SetLength(fileSize);
        }

        /// <summary>
        /// Flush data from memory to disk
        /// </summary>
        public void Flush()
        {
            _log.Write(Logger.DISK, "flush data from memory to disk");

            _stream.Flush();
        }

        /// <summary>
        /// Create new database based in _stream
        /// </summary>
        private void CreateDatabase(long initialSize)
        {
            // create a new header page in bytes (keep second page empty)
            var header = new HeaderPage
            {
                LastPageID = 1,
                Salt = AesEncryption.Salt()
            };

            // point to begin file
            _stream.Seek(0, SeekOrigin.Begin);

            // get header page in bytes
            var buffer = header.WritePage();

            _stream.Write(buffer, 0, BasePage.PAGE_SIZE);

            // if has initial size (at least 10 pages), alocate disk space now
            if (initialSize > (BasePage.PAGE_SIZE * 10))
            {
                _stream.SetLength(initialSize);
            }
        }

        public void Dispose()
        {
            if (_stream != null && _dispose)
            {
                _stream.Dispose();
            }
        }
    }
}