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
        private readonly CryptoStream _reader;
        private readonly CryptoStream _writer;

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

        public long StreamPosition => _stream.Position;

        public AesStream(string password, Stream stream)
        {
            _stream = stream;

            var isNew = _stream.Length == 0;

            // new file? create new salt
            if (isNew)
            {
                this.Salt = NewSalt();

                // first byte =1 means this datafile is encrypted
                _stream.WriteByte(1);
                _stream.Write(this.Salt, 0, ENCRYPTION_SALT_SIZE);
            }
            else
            {
                this.Salt = new byte[ENCRYPTION_SALT_SIZE];

                // checks if this datafile are encrypted
                var isEncrypted = _stream.ReadByte();

                if (isEncrypted != 1) throw new LiteException(0, "This file is not encrypted");

                _stream.Read(this.Salt, 0, ENCRYPTION_SALT_SIZE);
            }

            _aes = Aes.Create();
            _aes.Padding = PaddingMode.None;
            _aes.Mode = CipherMode.ECB;

            var pdb = new Rfc2898DeriveBytes(password, this.Salt);

            using (pdb as IDisposable)
            {
                _aes.Key = pdb.GetBytes(32);
                _aes.IV = pdb.GetBytes(16);
            }

            _encryptor = _aes.CreateEncryptor();
            _decryptor = _aes.CreateDecryptor();

            _reader = _stream.CanRead ?
                new CryptoStream(_stream, _decryptor, CryptoStreamMode.Read) :
                null;

            _writer = _stream.CanWrite ?
                new CryptoStream(_stream, _encryptor, CryptoStreamMode.Write) :
                null;

            // set stream to password checking
            _stream.Position = 32;

            var checkBuffer = new byte[32];

            // fill checkBuffer with encrypted 1 to check when open
            if (isNew)
            {
                checkBuffer.Fill(1, 0, checkBuffer.Length);

                _writer.Write(checkBuffer, 0, checkBuffer.Length);
            }
            else
            { 
                _reader.Read(checkBuffer, 0, checkBuffer.Length);

                if (!checkBuffer.All(x => x == 1))
                {
                    throw new LiteException(0, "Invalid password");
                }
            }

            _stream.Position = PAGE_SIZE;
        }

        /// <summary>
        /// Decrypt data from Stream
        /// </summary>
        public override int Read(byte[] array, int offset, int count)
        {
            ENSURE(count == PAGE_SIZE, "buffer size must be PAGE_SIZE");
            ENSURE(this.Position % PAGE_SIZE == 0, "position must be in PAGE_SIZE module");

            var r = _reader.Read(array, offset, count);

            return r;
        }

        /// <summary>
        /// Encrypt data to Stream
        /// </summary>
        public override void Write(byte[] array, int offset, int count)
        {
            ENSURE(count == PAGE_SIZE, "buffer size must be PAGE_SIZE");
            ENSURE(this.Position % PAGE_SIZE == 0, "position must be in PAGE_SIZE module");

            _writer.Write(array, offset, count);
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