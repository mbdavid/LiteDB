using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Implement blocked encryption/decyption in base Stream. Do read/write operations in blocks of PAGE_SIZE. Support Seek()
    /// Do not read/write first HEADER_PAGE_SIZE (contains plain data - salt + hash password)
    /// </summary>
    internal class AesStream : Stream
    {
        private readonly Stream _stream;
        private readonly Aes _aes;
        private readonly ICryptoTransform _encryptor;
        private readonly ICryptoTransform _decryptor;

        private long _position = 0;

        private long _blockPositionRead = -1;
        private byte[] _blockBufferRead = new byte[BasePage.PAGE_SIZE];

        private long _blockPositionWrite = 0;
        private byte[] _blockBufferWrite = new byte[BasePage.PAGE_SIZE];

        public AesStream(Stream stream, Aes aes)
        {
            _stream = stream;
            _aes = aes;
            _encryptor = _aes.CreateEncryptor();
            _decryptor = _aes.CreateDecryptor();
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
            var blockPosition = (_position / BasePage.PAGE_SIZE) * BasePage.PAGE_SIZE;
            var blockOffset = _position - blockPosition;

            // if are reading from different buffer position, read from _stream and decrypt block
            if (blockPosition != _blockPositionRead)
            {
                _blockPositionRead = blockPosition;

                var readCount = _blockPositionRead == 0 ? BasePage.PAGE_AVAILABLE_BYTES : BasePage.PAGE_SIZE;
                var readOffset = _blockPositionRead == 0 ? BasePage.PAGE_HEADER_SIZE : 0;

                // if position 0, clear first PAGE_HEADER_SIZE bytes
                if (_blockPositionRead == 0) Array.Clear(_blockBufferRead, 0, BasePage.PAGE_HEADER_SIZE);

                _stream.Position = _blockPositionRead + readOffset;

                var encrypted = new byte[readCount];
                _stream.Read(encrypted, 0, readCount);

                //using (var memory = new MemoryStream(encrypted))
                //{
                //    using (var crypto = new CryptoStream(memory, _decryptor, CryptoStreamMode.Read))
                //    {
                //        crypto.Read(_blockBufferRead, readOffset, readCount);
                //    }
                //}
                Buffer.BlockCopy(encrypted, 0, _blockBufferRead, readOffset, readCount);

            }

            // check if overflow block size
            if (blockOffset + count > BasePage.PAGE_SIZE) throw new InvalidOperationException("AesStream must read inside single block page");

            Buffer.BlockCopy(_blockBufferRead, (int)blockOffset, buffer, offset, count);

            // update position
            _position += count;

            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // caclulate block position
            var blockPosition = (_position / BasePage.PAGE_SIZE) * BasePage.PAGE_SIZE;

            // // if change from buffer position, must flush current write buffer into _stream
            // if (blockPosition != _blockPositionWrite)
            // {
            //     var needFlush = _blockPositionWrite >= 0;
            // 
            //     _blockPositionWrite = blockPosition;
            // 
            //     if (needFlush) this.FlushBuffer();
            // }

            var blockOffset = this.Position - _blockPositionWrite;

            // check if overflow block size
            if (blockOffset + count > BasePage.PAGE_SIZE) throw new InvalidOperationException("AesStream must write inside single block page");

            Buffer.BlockCopy(buffer, offset, _blockBufferWrite, (int)blockOffset, count);

            // update position using buffer size
            _position += count;

            // if position exceed for next block, flush now
            if (_position == _blockPositionWrite + BasePage.PAGE_SIZE)
            {
                this.FlushBuffer();
                _blockPositionWrite += BasePage.PAGE_SIZE;
            }
        }

        public override void Flush()
        {
        }

        private void FlushBuffer()
        {
            var writeCount = _blockPositionWrite == 0 ? BasePage.PAGE_AVAILABLE_BYTES : BasePage.PAGE_SIZE;
            var writeOffset = _blockPositionWrite == 0 ? BasePage.PAGE_HEADER_SIZE : 0;

            // encrypt write buffer and write on stream (must use memorystream)
            using (var memory = new MemoryStream(BasePage.PAGE_SIZE))
            {
                using (var crypto = new CryptoStream(memory, _encryptor, CryptoStreamMode.Write))
                {
                    crypto.Write(_blockBufferWrite, writeOffset, writeCount);
                    crypto.FlushFinalBlock();
                }

                //var encrypted = memory.ToArray();
                var encrypted = new byte[writeCount];
                Buffer.BlockCopy(_blockBufferWrite, writeOffset, encrypted, 0, writeCount);

                _stream.Position = _blockPositionWrite + writeOffset;

                _stream.Write(encrypted, 0, writeCount);

                _stream.Flush();
            }

            // clear write buffer
            Array.Clear(_blockBufferWrite, 0, BasePage.PAGE_SIZE);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.FlushBuffer();

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