using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using XTSSharp;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Encrypted AES Stream
    /// </summary>
    public class AesStream : Stream
    {
        private readonly Stream _baseStream;

        private readonly Stream _aesStream;

        public Stream BaseStream => _baseStream;

        public AesStream(string password, Stream stream, bool initialize = false)
        {
            var isNew = stream.Length == 0 || initialize;
            var encryption = EncryptionType.AesXts;
            var salt = new byte[ENCRYPTION_SALT_SIZE];

            _baseStream = stream;
            _baseStream.Position = 0;

            try
            {
                if (isNew)
                {
                    // create new SALT
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(salt);
                    }

                    // store encryption type + salt
                    _baseStream.WriteByte((byte)encryption);
                    _baseStream.Write(salt, 0, ENCRYPTION_SALT_SIZE);

                    // fill all page with 0
                    var left = PAGE_SIZE - ENCRYPTION_SALT_SIZE - 1;

                    _baseStream.Write(new byte[left], 0, left);
                }
                else
                {
                    // read EncryptionMode byte
                    encryption = (EncryptionType)_baseStream.ReadByte();

                    // read salt
                    _baseStream.Read(salt, 0, ENCRYPTION_SALT_SIZE);
                }

                // initialize encryption stream (xts/ecb)
                switch (encryption)
                {
                    case EncryptionType.None:
                        throw new LiteException(0, "File is not encrypted.");
                    case EncryptionType.AesEcb:
                        _aesStream = new AesEcbStream(password, _baseStream, salt);
                        break;
                    case EncryptionType.AesXts:
                        _aesStream = CreateXtsStream(password, _baseStream, salt);
                        break;
                    default:
                        throw new LiteException(0, "Unsupported encryption mode.");
                }
            }
            catch
            {
                _aesStream?.Dispose();
                _baseStream.Dispose();
                throw;
            }
        }

        #region Stream implementations

        public override bool CanRead => _aesStream.CanRead;

        public override bool CanSeek => _aesStream.CanSeek;

        public override bool CanWrite => _aesStream.CanWrite;

        public override long Length => _aesStream.Length - PAGE_SIZE;

        public override long Position 
        { 
            get => _aesStream.Position - PAGE_SIZE; 
            set => this.Seek(value, SeekOrigin.Begin); 
        }

        public override void Flush() => _aesStream.Flush();

        public override long Seek(long offset, SeekOrigin origin) => _aesStream.Seek(offset + PAGE_SIZE, origin);

        public override void SetLength(long value) => _aesStream.SetLength(value + PAGE_SIZE);

        public override int Read(byte[] buffer, int offset, int count) => _aesStream.Read(buffer, offset, count);

        public override void Write(byte[] buffer, int offset, int count) => _aesStream.Write(buffer, offset, count);

        #endregion

        /// <summary>
        /// Create a new instance of XtsStream using PAGE_SIZE sector size
        /// </summary>
        public static XtsSectorStream CreateXtsStream(string password, Stream baseStream, byte[] salt)
        {
            using (var pdb = new Rfc2898DeriveBytes(password, salt))
            {
                var xts = XtsAes128.Create(pdb.GetBytes(32));
                return new XtsSectorStream(baseStream, xts, PAGE_SIZE, 0);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _aesStream.Dispose();
            _baseStream.Dispose();
        }
    }
}