using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace LiteDB_V6
{
    /// <summary>
    /// Implement NTFS File disk
    /// </summary>
    internal class FileDiskService : IDisposable
    {
        /// <summary>
        /// Position, on page, about page type
        /// </summary>
        private const int PAGE_TYPE_POSITION = 4;

        private Stream _stream;
        private RijndaelEncryption _crypto = null;

        public FileDiskService(Stream stream, string password)
        {
            _stream = stream;

            if (password != null)
            {
                _crypto = new RijndaelEncryption(password);
            }
        }

        public void Dispose()
        {
            if(_crypto != null)
            {
                _crypto.Dispose();
            }
        }

        /// <summary>
        /// Read page bytes from disk
        /// </summary>
        public virtual byte[] ReadPage(uint pageID)
        {
            var buffer = new byte[BasePage.PAGE_SIZE];
            var position = BasePage.GetSizeOfPages(pageID);

            // position cursor
            if (_stream.Position != position)
            {
                _stream.Seek(position, SeekOrigin.Begin);
            }

            // read bytes from data file
            _stream.Read(buffer, 0, BasePage.PAGE_SIZE);

            // when reading the header, check the password
            if (pageID == 0 && _crypto != null)
            {
                // I know, header page will be double read (it's the price for isolated concerns)
                var header = (HeaderPage)BasePage.ReadPage(buffer);

                if (LiteDB.BinaryExtensions.BinaryCompareTo(_crypto.HashPassword, header.Password) != 0)
                {
                    throw LiteDB.LiteException.DatabaseWrongPassword();
                }
            }
            else if (_crypto != null)
            {
                buffer = _crypto.Decrypt(buffer);
            }

            return buffer;
        }
    }
}