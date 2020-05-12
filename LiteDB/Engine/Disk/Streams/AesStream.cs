using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using XTSSharp;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Encrypted AES Stream
    /// </summary>
    public class AesStream : Stream
    {
        private readonly Stream _baseAesStream;
        public Stream BaseStream => _baseAesStream is AesXtsStream xts ? xts.BaseStream :
                                    _baseAesStream is AesEcbStream ecb ? ecb.BaseStream : null;

        public AesStream(string password, Stream stream, bool initialize = false)
        {
            var encryption = (EncryptionType)stream.ReadByte();

            var isNew = stream.Length == 0 || initialize;
            if(isNew)
            {
                _baseAesStream = new AesXtsStream(password, stream, initialize);
                return;
            }

            stream.Seek(0, SeekOrigin.Begin);

            switch (encryption)
            {
                case EncryptionType.None:
                    throw new LiteException(0, "File is not encrypted.");
                case EncryptionType.AesEcb:
                    _baseAesStream = new AesEcbStream(password, stream, initialize);
                    break;
                case EncryptionType.AesXts:
                    _baseAesStream = new AesXtsStream(password, stream, initialize);
                    break;
                default:
                    throw new LiteException(0, "Unsupported encryption mode.");
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

        #region Stream implementations
        public override bool CanRead => _baseAesStream.CanRead;

        public override bool CanSeek => _baseAesStream.CanSeek;

        public override bool CanWrite => _baseAesStream.CanWrite;

        public override long Length => _baseAesStream.Length - PAGE_SIZE;

        public override long Position 
        { 
            get => _baseAesStream.Position - PAGE_SIZE; 
            set => this.Seek(value, SeekOrigin.Begin); 
        }

        public override void Flush() => _baseAesStream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _baseAesStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _baseAesStream.Seek(offset + PAGE_SIZE, origin);

        public override void SetLength(long value) => _baseAesStream.SetLength(value + PAGE_SIZE);

        public override void Write(byte[] buffer, int offset, int count) => _baseAesStream.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _baseAesStream.Dispose();
        }
        #endregion
    }
}