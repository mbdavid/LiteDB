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
        private readonly Stream _stream;
        private readonly Xts _xts;
        private readonly XtsSectorStream _xtsStream;

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

        public AesStream(string password, Stream stream, bool initialize = false)
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

                using (var pdb = new Rfc2898DeriveBytes(password, this.Salt))
                {
                    _xts = XtsAes128.Create(pdb.GetBytes(32));
                }

                _xtsStream = new XtsSectorStream(_stream, _xts, PAGE_SIZE, 0);

                if(!isNew)
                {
                    //check if password is correct using the HEADER_INFO string;
                    var header = new byte[PAGE_SIZE];
                    var headerInfoBytes = new byte[System.Text.Encoding.UTF8.GetByteCount(HeaderPage.HEADER_INFO)];

                    _xtsStream.Position = PAGE_SIZE;
                    _xtsStream.Read(header, 0, PAGE_SIZE);

                    Array.Copy(header, HeaderPage.P_HEADER_INFO, headerInfoBytes, 0, headerInfoBytes.Length);

                    var headerInfoStr = System.Text.Encoding.UTF8.GetString(headerInfoBytes);

                    if(HeaderPage.HEADER_INFO != headerInfoStr)
                        throw new LiteException(0, "Invalid password");
                }

                _xtsStream.Position = PAGE_SIZE;

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

            return _xtsStream.Read(array, offset, count);
        }

        /// <summary>
        /// Encrypt data to Stream
        /// </summary>
        public override void Write(byte[] array, int offset, int count)
        {
            ENSURE(count == PAGE_SIZE, "buffer size must be PAGE_SIZE");
            ENSURE(this.Position % PAGE_SIZE == 0, "position must be in PAGE_SIZE module");

            _xtsStream.Write(array, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _xtsStream?.Dispose();
            _stream?.Dispose();
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
            _xtsStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _xtsStream.Seek(offset + PAGE_SIZE, origin);
        }

        public override void SetLength(long value)
        {
            _xtsStream.SetLength(value + PAGE_SIZE);
        }
    }
}