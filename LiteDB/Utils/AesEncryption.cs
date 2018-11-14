using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using LiteDB.Engine;
using static LiteDB.Constants;
using System.Buffers;

namespace LiteDB
{
    /// <summary>
    /// Encryption AES wrapper to encrypt database pages
    /// </summary>
    internal class AesEncryption : IDisposable
    {
        private readonly Aes _aes;
        private readonly ICryptoTransform _encryptor;
        private readonly ICryptoTransform _decryptor;

        public byte[] Salt { get; private set; }

        public AesEncryption(string password, byte[] salt)
        {
            this.Salt = salt;

            _aes = Aes.Create();
            _aes.Padding = PaddingMode.Zeros;

            var pdb = new Rfc2898DeriveBytes(password, salt);

            using (pdb as IDisposable)
            {
                _aes.Key = pdb.GetBytes(32);
                _aes.IV = pdb.GetBytes(16);
            }

            _encryptor = _aes.CreateEncryptor();
            _decryptor = _aes.CreateDecryptor();
        }

        /// <summary>
        /// Read buffer array writing encrypted data into output Stream
        /// </summary>
        public void Encrypt(ArraySlice<byte> input, Stream output)
        {
            using (var stream = new MemoryStream(input.Array, input.Offset, input.Count))
            using (var crypto = new CryptoStream(stream, _encryptor, CryptoStreamMode.Read))
            {
                var arr = ArrayPool<byte>.Shared.Rent(input.Count);

                crypto.Read(arr, 0, input.Count);
                crypto.FlushFinalBlock();

                output.Write(arr, 0, input.Count);

                ArrayPool<byte>.Shared.Return(arr);
            }
        }

        /// <summary>
        /// Read encrypted input and write plain data into buffer array slice
        /// </summary>
        public void Decrypt(Stream input, ArraySlice<byte> buffer)
        {
            using (var crypto = new CryptoStream(input, _decryptor, CryptoStreamMode.Write))
            {
                crypto.Write(buffer.Array, buffer.Offset, buffer.Count);
                crypto.FlushFinalBlock();
            }
        }

        /// <summary>
        /// Get new salt for encryption
        /// </summary>
        public static byte[] NewSalt()
        {
            var salt = new byte[ENCRYPTION_SALT_SIZE];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            return salt;
        }

        /// <summary>
        /// Get new AesEncryption based on disk factory
        /// </summary>
        public static AesEncryption CreateAes(string password, IDiskFactory factory)
        {
            if (factory.Exists() == false) return new AesEncryption(password, NewSalt());

            var stream = factory.GetStream(false, false);

            try
            {
                var salt = new byte[ENCRYPTION_SALT_SIZE];

                stream.Position = P_HEADER_SALT;
                stream.Read(salt, 0, ENCRYPTION_SALT_SIZE);
                //TODO: testar a senha aqui?

                return new AesEncryption(password, salt);
            }
            finally
            {
                if (factory.CloseOnDispose)
                {
                    stream.Dispose();
                }
            }
        }

        public void Dispose()
        {
            _encryptor.Dispose();
            _decryptor.Dispose();
        }
    }
}
