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
                var buffer = ArrayPool<byte>.Shared.Rent(input.Count);

                crypto.Read(buffer, 0, input.Count);
                //crypto.FlushFinalBlock();

                output.Write(buffer, 0, input.Count);

                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Read encrypted input and write plain data into buffer array slice
        /// </summary>
        public void Decrypt(Stream input, ArraySlice<byte> output)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(output.Count);

            input.Read(buffer, 0, output.Count);

            using (var stream = new MemoryStream(buffer))
            using (var crypto = new CryptoStream(stream, _decryptor, CryptoStreamMode.Read))
            {
                crypto.Read(output.Array, output.Offset, output.Count);
            }

            ArrayPool<byte>.Shared.Return(buffer);
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
        /// Get new AesEncryption instance. Use Stream to read SALT (at end of first page) and checks if password are correct
        /// </summary>
        public static AesEncryption CreateAes(StreamPool pool, string password)
        {
            if (pool.Factory.Exists() == false) return new AesEncryption(password, NewSalt());

            var stream = pool.Rent();

            try
            {
                var salt = new byte[ENCRYPTION_SALT_SIZE];

                stream.Position = P_ENCRYPTION_SALT;
                stream.Read(salt, 0, ENCRYPTION_SALT_SIZE);

                return new AesEncryption(password, salt);
            }
            finally
            {
                pool.Return(stream);
            }
        }

        public void Dispose()
        {
            _encryptor.Dispose();
            _decryptor.Dispose();
        }
    }
}
