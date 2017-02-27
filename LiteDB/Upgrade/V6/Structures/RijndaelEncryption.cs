using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using LiteDB;

namespace LiteDB_V6
{
    /// <summary>
    /// Simple Rijndael wrapper to encrypt data pages (based in http://stackoverflow.com/questions/165808/simple-two-way-encryption-for-c-sharp)
    /// </summary>
    internal class RijndaelEncryption
    {
        private static readonly byte[] SALT = new byte[] { 0x16, 0xae, 0xbf, 0x20, 0x01, 0xa0, 0xa9, 0x52, 0x34, 0x1a, 0x45, 0x55, 0x4a, 0xe1, 0x32, 0x1d };

#if NET35
        private Rijndael _rijndael;
#else
        private Aes _rijndael;
#endif

        public byte[] HashPassword { get; private set; }

        public RijndaelEncryption(string password)
        {
#if NET35
            _rijndael = Rijndael.Create();
#else
            _rijndael = Aes.Create();
#endif
            _rijndael.Padding = PaddingMode.Zeros;
            Rfc2898DeriveBytes pdb = null;

            this.HashPassword = HashSHA1(password);

            try
            {
                pdb = new Rfc2898DeriveBytes(password, SALT);
                _rijndael.Key = pdb.GetBytes(32);
                _rijndael.IV = pdb.GetBytes(16);
            }
            finally
            {
                IDisposable disp = pdb as IDisposable;

                if (disp != null)
                {
                    disp.Dispose();
                }
            }
        }

        /// <summary>
        /// Decrypt and byte array returning a new byte array
        /// </summary>
        public byte[] Decrypt(byte[] encryptedValue)
        {
            using (var decryptor = _rijndael.CreateDecryptor())
            using (var stream = new MemoryStream())
            using (var crypto = new CryptoStream(stream, decryptor, CryptoStreamMode.Write))
            {
                crypto.Write(encryptedValue, 0, encryptedValue.Length);
                crypto.FlushFinalBlock();
                stream.Position = 0;
                var decryptedBytes = new Byte[stream.Length];
                stream.Read(decryptedBytes, 0, decryptedBytes.Length);

                return decryptedBytes;
            }
        }

        /// <summary>
        /// Hash a password using SHA1 just to verify password
        /// </summary>
        public static byte[] HashSHA1(string password)
        {
#if NET35
            var sha = new SHA1CryptoServiceProvider();
#else
            var sha = SHA1.Create();
#endif

            var shaBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return shaBytes;
        }

        public void Dispose()
        {
            if (_rijndael != null)
            {
#if NET35
                _rijndael.Clear();
#endif
                _rijndael = null;
            }
        }
    }
}