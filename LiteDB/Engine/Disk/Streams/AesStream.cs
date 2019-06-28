using System;
using System.IO;
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

            // new file? create new salt
            if (_stream.Length == 0)
            {
                this.Salt = NewSalt();

                _stream.Write(this.Salt, 0, ENCRYPTION_SALT_SIZE);
            }
            else
            {
                this.Salt = new byte[ENCRYPTION_SALT_SIZE];

                _stream.Read(this.Salt, 0, ENCRYPTION_SALT_SIZE);
            }

            _stream.Position = PAGE_SIZE;

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
        }

        /// <summary>
        /// Decrypt data from Stream
        /// </summary>
        public override int Read(byte[] array, int offset, int count)
        {
            ENSURE(count == PAGE_SIZE, "buffer size must be PAGE_SIZE");
            ENSURE(this.Position % PAGE_SIZE == 0, "position must be in PAGE_SIZE module");

            var r = _reader.Read(array, offset, count);

            //if (!_reader.HasFlushedFinalBlock)
                //_reader.FlushFinalBlock();

            return r;
        }

        /// <summary>
        /// Encrypt data to Stream
        /// </summary>
        public override void Write(byte[] array, int offset, int count)
        {
            ENSURE(count == PAGE_SIZE, "buffer size must be PAGE_SIZE");
            ENSURE(this.Position % PAGE_SIZE == 0, "position must be in PAGE_SIZE module");

            //if (!_writer.HasFlushedFinalBlock)
            //    _writer.FlushFinalBlock();

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