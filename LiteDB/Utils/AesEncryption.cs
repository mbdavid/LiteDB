using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Encryption AES wrapper to encrypt data pages
    /// </summary>
    internal class AesEncryption : IDisposable
    {
        private Aes _aes;
        private ICryptoTransform _encryptor;
        private ICryptoTransform _decryptor;

        public AesEncryption(string password, byte[] salt)
        {
            _aes = Aes.Create();
            _aes.Padding = PaddingMode.Zeros;

            using (var pdb = new Rfc2898DeriveBytes(password, salt))
            {
                _aes.Key = pdb.GetBytes(32);
                _aes.IV = pdb.GetBytes(16);
            }

            _encryptor = _aes.CreateEncryptor();
            _decryptor = _aes.CreateDecryptor();
        }

        public Stream CreateEncryptorStream(Stream stream)
        {
            return new CryptoStream(stream, _encryptor, CryptoStreamMode.Write);
        }

        public Stream CreateDecryptorStream(Stream stream)
        {
            return new CryptoStream(stream, _decryptor, CryptoStreamMode.Write);
        }

        /// <summary>
        /// Hash a password using PBKDF2 hash
        /// </summary>
        public static byte[] HashPBKDF2(string password, byte[] salt)
        {
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt)
            {
                IterationCount = 1000
            };

            return pbkdf2.GetBytes(20);
        }

        /// <summary>
        /// Generate a salt key that will be stored inside first page database
        /// </summary>
        public static byte[] Salt(int maxLength = 16)
        {
            var salt = new byte[maxLength];

            using (var rng = RandomNumberGenerator.Create())
            { 
                rng.GetBytes(salt);
            }

            return salt;
        }

        public void Dispose()
        {
            if (_aes != null)
            {
                _aes.Dispose();
                _encryptor.Dispose();
                _decryptor.Dispose();
            }
        }
    }
}