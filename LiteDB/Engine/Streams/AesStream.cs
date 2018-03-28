using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Implement blocked encryption/decyption in base Stream. Do read/write operations in blocks of PAGE_SIZE. Support Seek()
    /// </summary>
    public class AesStream : Stream
    {
        private Stream _stream;
        private Aes _aes;
        private ICryptoTransform _encryptor;
        private ICryptoTransform _decryptor;

        private long _position = 0;

        private long _blockPositionRead = -1;
        private byte[] _blockBufferRead = new byte[BasePage.PAGE_SIZE];

        private long _blockPositionWrite = -1;
        private byte[] _blockBufferWrite = new byte[BasePage.PAGE_SIZE];


        public AesStream(Stream stream, Aes aes)
        {
            _stream = stream;
            _aes = aes;
            _aes.CreateEncryptor();
            _aes.CreateDecryptor();
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position { get => _position; set => _position = value; }

        public override void SetLength(long value) => _stream.SetLength(value);

        public override long Seek(long offset, SeekOrigin origin)
        {
            // update fake stream _position based on offset/origin
            _position =
                origin == SeekOrigin.Begin ? offset :
                origin == SeekOrigin.Current ? _position + offset :
                _position - offset;

            return _position;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // caclulate block position (0, 8192, 16384, ...)
            var blockPosition = (this.Position / BasePage.PAGE_SIZE) * BasePage.PAGE_SIZE;
            var blockOffset = this.Position - _blockPositionRead;

            // if are reading from different buffer position, read from _stream and decrypt block
            if (blockPosition != _blockPositionRead)
            {
                _stream.Position = _blockPositionRead;

                using (var crypto = new CryptoStream(_stream, _decryptor, CryptoStreamMode.Read))
                {
                    crypto.Read(_blockBufferRead, 0, BasePage.PAGE_SIZE);
                }
            }

            // check if overflow block size
            if (blockOffset + count > BasePage.PAGE_SIZE) throw new InvalidOperationException("AesStream must write inside single block page");

            Buffer.BlockCopy(_blockBufferRead, (int)blockOffset, buffer, offset, count);

            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // caclulate block position
            var blockPosition = (this.Position / BasePage.PAGE_SIZE) * BasePage.PAGE_SIZE;

            // if change from buffer position, must flush current write buffer into _stream
            if (blockPosition != _blockPositionWrite)
            {
                this.Flush();

                // update write block position and clear cache
                _blockPositionWrite = blockPosition;
                _blockBufferWrite = new byte[BasePage.PAGE_SIZE];
            }

            var blockOffset = this.Position - _blockPositionWrite;

            // check if overflow block size
            if (blockOffset + count > BasePage.PAGE_SIZE) throw new InvalidOperationException("AesStream must write inside single block page");

            Buffer.BlockCopy(buffer, offset, _blockBufferWrite, (int)blockOffset, count);
        }

        public override void Flush()
        {
            // when buffer position is -1 mean not initialized - just exit flush
            if (_blockPositionWrite == -1) return;

            _stream.Position = _blockPositionWrite;

            // encrypt write buffer and write on stream
            using (var crypto = new CryptoStream(_stream, _encryptor, CryptoStreamMode.Write))
            {
                crypto.Write(_blockBufferWrite, 0, BasePage.PAGE_SIZE);
                crypto.FlushFinalBlock();
            }

            _stream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Flush();

                _encryptor?.Dispose();
                _decryptor?.Dispose();
                _aes?.Dispose();
                _stream?.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Static helpers

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

        #endregion
    }
}