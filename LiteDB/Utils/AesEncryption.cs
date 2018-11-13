using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using LiteDB.Engine;
using static LiteDB.Constants;

namespace LiteDB
{
    /// <summary>
    /// Encryption AES wrapper to encrypt data pages
    /// </summary>
    internal class AesEncryption
    {
        private Aes _aes;

        public AesEncryption(string password, byte[] salt)
        {
            _aes = Aes.Create();
            _aes.Padding = PaddingMode.Zeros;

            var pdb = new Rfc2898DeriveBytes(password, salt);

            using (pdb as IDisposable)
            {
                _aes.Key = pdb.GetBytes(32);
                _aes.IV = pdb.GetBytes(16);
            }
        }

        /// <summary>
        /// Encrypt buffer array overriding internal data
        /// </summary>
        public void Encrypt(ArraySlice<byte> buffer)
        {
            //using (var encryptor = _aes.CreateEncryptor())
            //using (var stream = new MemoryStream())
            //using (var crypto = new CryptoStream(stream, encryptor, CryptoStreamMode.Write))
            //{
            //    crypto.Write(bytes, 0, bytes.Length);
            //    crypto.FlushFinalBlock();
            //    stream.Position = 0;
            //    var encrypted = new byte[stream.Length];
            //    stream.Read(encrypted, 0, encrypted.Length);
            //
            //    return encrypted;
            //}
        }

        /// <summary>
        /// Decrypt current buffer overriding internal data
        /// </summary>
        public void Decrypt(ArraySlice<byte> buffer)
        {
            //using (var decryptor = _aes.CreateDecryptor())
            //using (var stream = new MemoryStream())
            //using (var crypto = new CryptoStream(stream, decryptor, CryptoStreamMode.Write))
            //{
            //    crypto.Write(encryptedValue, 0, encryptedValue.Length);
            //    crypto.FlushFinalBlock();
            //    stream.Position = 0;
            //    var decryptedBytes = new Byte[stream.Length];
            //    stream.Read(decryptedBytes, 0, decryptedBytes.Length);
            //
            //    return decryptedBytes;
            //}
        }

        /// <summary>
        /// Get new salt in buffer
        /// </summary>
        public static void Salt(ArraySlice<byte> buffer)
        {
            var salt = new byte[ENCRYPTION_SALT_SIZE];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            Buffer.BlockCopy(salt, 0, buffer.Array, buffer.Offset, ENCRYPTION_SALT_SIZE);
        }

        public void Dispose()
        {
            if (_aes != null)
            {
                _aes = null;
            }
        }
    }
}
