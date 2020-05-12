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
    public class AesEcbStream : Stream
    {
        private readonly Aes _aes;
        private readonly ICryptoTransform _encryptor;
        private readonly ICryptoTransform _decryptor;

        private readonly Stream _stream;
        private readonly CryptoStream _reader;
        private readonly CryptoStream _writer;

        public Stream BaseStream => _stream;
        public byte[] Salt { get; }

        public override long Position
        {
            get => _stream.Position;
            set => this.Seek(value, SeekOrigin.Begin);
        }

        public AesEcbStream(string password, Stream stream, bool initialize = false)
        {
            _stream = stream;
            _stream.Position = 0;

            var isNew = _stream.Length == 0 || initialize;

            try
            {
                // new file? create new salt
                if (isNew)
                {
                    this.Salt = AesStream.NewSalt();

                    _stream.WriteByte((byte)EncryptionType.AesEcb);
                    _stream.Write(this.Salt, 0, ENCRYPTION_SALT_SIZE);

                    // fill all page with 0
                    var left = PAGE_SIZE - ENCRYPTION_SALT_SIZE - 1;

                    _stream.Write(new byte[left], 0, left);
                }
                else
                {
                    this.Salt = new byte[ENCRYPTION_SALT_SIZE];

                    //skip EncryptionMode byte
                    _stream.ReadByte();

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
            catch
            {
                _stream.Dispose();

                throw;
            }
        }

        #region Stream implementations
        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

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

        public override void Flush() => _stream.Flush();

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        public override void SetLength(long value) => _stream.SetLength(value);
        #endregion
    }
}