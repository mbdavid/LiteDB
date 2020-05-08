using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Encrypted AES Stream
    /// </summary>
    public class AesStream : Stream
    {
        private readonly Aes _aes;
        private readonly ICryptoTransform _encryptor;
        private readonly ICryptoTransform _decryptor;

        private readonly Stream _stream;

        /// <summary>
        /// Get plain stream
        /// </summary>
        public Stream BaseStream => _stream;

        public byte[] Salt { get; }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length - PAGE_SIZE;

        public override long Position
        {
            get => _stream.Position - PAGE_SIZE;
            set => this.Seek(value, SeekOrigin.Begin);
        }

        public AesStream(string password, Stream stream, bool initialize = false, CipherMode cipherMode = CipherMode.ECB)
        {
            _stream = stream;
            _stream.Position = 0;

            var isNew = _stream.Length == 0 || initialize;

            try
            {
                // new file? create new salt
                if (isNew)
                {
                    this.Salt = NewSalt();

                    _stream.WriteByte(1);
                    _stream.Write(this.Salt, 0, ENCRYPTION_SALT_SIZE);

                    // fill all page with 0
                    var left = PAGE_SIZE - ENCRYPTION_SALT_SIZE - 1;

                    _stream.Write(new byte[left], 0, left);
                }
                else
                {
                    this.Salt = new byte[ENCRYPTION_SALT_SIZE];

                    // checks if this datafile are encrypted
                    var isEncrypted = _stream.ReadByte();

                    if (isEncrypted != 1)
                    {
                        throw new LiteException(0, "This file is not encrypted");
                    }

                    _stream.Read(this.Salt, 0, ENCRYPTION_SALT_SIZE);
                }

                _aes = Aes.Create();
                _aes.Padding = PaddingMode.None;
                _aes.Mode = cipherMode;

                var pdb = new Rfc2898DeriveBytes(password, this.Salt);

                using (pdb as IDisposable)
                {
                    _aes.Key = pdb.GetBytes(32);
                    _aes.IV = pdb.GetBytes(16);
                }

                _encryptor = _aes.CreateEncryptor();
                _decryptor = _aes.CreateDecryptor();

                // set stream to password checking
                _stream.Position = 32;

                var checkBuffer = new byte[32];

                // fill checkBuffer with encrypted 1 to check when open
                if (isNew)
                {
                    checkBuffer.Fill(1, 0, checkBuffer.Length);
                    var encryptedOnes = _encryptor.TransformFinalBlock(checkBuffer, 0, checkBuffer.Length);
                    _stream.Write(encryptedOnes, 0, encryptedOnes.Length);
                }
                else
                {
                    var encryptedOnes = new byte[checkBuffer.Length];
                    _stream.Read(encryptedOnes, 0, checkBuffer.Length);
                    var decryptedOnes = _decryptor.TransformFinalBlock(encryptedOnes, 0, encryptedOnes.Length);

                    if (!decryptedOnes.All(x => x == 1))
                    {
                        throw new LiteException(0, "Invalid password");
                    }
                }

                _stream.Position = PAGE_SIZE;

            }
            catch
            {
                _stream.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Decrypt data from Stream
        /// </summary>
        public override int Read(byte[] array, int offset, int count)
        {
            ENSURE(count == PAGE_SIZE, "buffer size must be PAGE_SIZE");
            ENSURE(this.Position % PAGE_SIZE == 0, "position must be in PAGE_SIZE module");

            var encryptedBuf = new byte[PAGE_SIZE];
            _stream.Read(encryptedBuf, 0, PAGE_SIZE);

            using (var ms = new MemoryStream(encryptedBuf))
            using (var reader = new CryptoStream(ms, _decryptor, CryptoStreamMode.Read))
            {
                var readBytes = reader.Read(array, offset, count);
                return readBytes;
            }
        }

        /// <summary>
        /// Encrypt data to Stream
        /// </summary>
        public override void Write(byte[] array, int offset, int count)
        {
            ENSURE(count == PAGE_SIZE, "buffer size must be PAGE_SIZE");
            ENSURE(this.Position % PAGE_SIZE == 0, "position must be in PAGE_SIZE module");

            var decryptedBuf = new byte[PAGE_SIZE];
            var encryptedBuf = new byte[PAGE_SIZE];
            Array.Copy(array, offset, decryptedBuf, 0, PAGE_SIZE);

            using (var ms = new MemoryStream(decryptedBuf))
            using (var enc = new CryptoStream(ms, _encryptor, CryptoStreamMode.Read))
            {
                enc.Read(encryptedBuf, 0, PAGE_SIZE);
            }

            _stream.Write(encryptedBuf, 0, PAGE_SIZE);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _stream?.Dispose();

            _encryptor.Dispose();
            _decryptor.Dispose();

            _aes.Dispose();
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

        public override void Flush()
        {
            _stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset + PAGE_SIZE, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value + PAGE_SIZE);
        }
    }
}