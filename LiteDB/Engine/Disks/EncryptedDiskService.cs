using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Encrypted implementation of FileDiskService
    /// Header page is not encrypted to simple checks and store salt (contains SHA1 hash password for validate)
    /// </summary>
    public class EncryptedDiskService : FileDiskService
    {
        private SimpleAES _crypto;
        private string _password;
        private byte[] _sha1;

        public EncryptedDiskService(string filename, string password, bool journal = true)
            : this(filename, password, new FileOptions { Journal = journal })
        {
        }

        public EncryptedDiskService(string filename, string password, FileOptions options)
            : base(filename, options)
        {
            _password = password;
            _sha1 = SimpleAES.HashSHA1(_password);
        }

        internal override HeaderPage InitializeHeaderPage()
        {
            // creating header page with password in sha1 and salt key
            return new HeaderPage()
            {
                Password = _sha1,
                Salt = SimpleAES.Salt()
            };
        }

        public override void Open()
        {
            base.Open();

            // checks password
            var header = BasePage.ReadPage(this.ReadPage(0)) as HeaderPage;

            // compare header password with user password
            if (_sha1.BinaryCompareTo(header.Password) != 0)
            {
                this.Dispose();
                throw LiteException.DatabaseWrongPassword();
            }

            // initialize crypto
            _crypto = new SimpleAES(_password, header.Salt);

            // clear plain-text password from memory
            _password = null;
        }

        /// <summary>
        /// Override read page decrypting data from disk
        /// </summary>
        public override byte[] ReadPage(uint pageID)
        {
            var buffer = base.ReadPage(pageID);

            if (pageID == 0)
            {
                return buffer;
            }

            return _crypto.Decrypt(buffer);
        }

        /// <summary>
        /// Override write page to write encrypted data
        /// </summary>
        public override void WritePage(uint pageID, byte[] buffer)
        {
            if (pageID == 0)
            {
                base.WritePage(pageID, buffer);
            }
            else
            {
                base.WritePage(pageID, _crypto.Encrypt(buffer));
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            if(_crypto != null)
            {
                _crypto.Dispose();
            }
        }
    }
}