using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Encrypted implementation of FileDiskService
    /// Header page is not encrypted to simple checks (contains SHA1 hash password for validate)
    /// Uses AES - Rijndael implementation for symmetric encrypt. There is not password change
    /// </summary>
    internal class EncryptedDiskService : FileDiskService
    {
        private IEncryption _crypto;

        private byte[] _password;

        public EncryptedDiskService(ConnectionString conn, Logger log)
            : base(conn, log)
        {
            // initialize AES with passoword
            var password = conn.GetValue<string>("password", null);

            // hash password to store in header to check if password is correct
            _crypto = LitePlatform.Platform.GetEncryption(password);

            _password = _crypto.HashSHA1(password);
        }

        protected override HeaderPage CreateHeaderPage()
        {
            return new HeaderPage() { Password = _password };
        }

        protected override void ValidatePassword(byte[] passwordHash)
        {
            if (passwordHash.BinaryCompareTo(_password) != 0)
            {
                throw LiteException.DatabaseWrongPassword();
            }
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
            _crypto.Dispose();
        }
    }
}