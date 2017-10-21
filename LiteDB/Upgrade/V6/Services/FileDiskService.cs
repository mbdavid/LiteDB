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

        /// <summary>
        /// LiteDB v2 fixed salt
        /// </summary>
        private static byte[] SALT = new byte[] { 0x16, 0xae, 0xbf, 0x20, 0x01, 0xa0, 0xa9, 0x52, 0x34, 0x1a, 0x45, 0x55, 0x4a, 0xe1, 0x32, 0x1d };

        private Stream _stream;
        private LiteDB.AesEncryption _crypto = null;
        private byte[] _password = null;

        public FileDiskService(Stream stream, string password)
        {
            _stream = stream;

            if (password != null)
            {
                _crypto = new LiteDB.AesEncryption(password, SALT);
                _password = LiteDB.AesEncryption.HashSHA1(password);
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

                if (LiteDB.BinaryExtensions.BinaryCompareTo(_password, header.Password) != 0)
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