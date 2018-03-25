using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Implement seekable encrypted AES Stream (CryptoStream do not support Seek)
    /// Based on https://stackoverflow.com/questions/5026409/how-to-add-seek-and-position-capabilities-to-cryptostream
    /// </summary>
    public class AesStream : Stream
    {
        private Stream _stream;
        private Aes _aes;
        private ICryptoTransform _encryptor;

        public AesStream(Stream stream, string password, byte[] salt)
        {
            _stream = stream;

            using (var key = new PasswordDeriveBytes(password, salt))
            {
                _aes = Aes.Create();
                _aes.Padding = PaddingMode.Zeros;

                using (var pdb = new Rfc2898DeriveBytes(password, salt))
                {
                    _aes.Key = pdb.GetBytes(32);
                    _aes.IV = pdb.GetBytes(16);
                }

                _encryptor = _aes.CreateEncryptor(_aes.Key, _aes.IV);
            }
        }

        private void Cipher(byte[] buffer, int offset, int count, long streamPos)
        {
            //find block number
            var blockSizeInByte = _aes.BlockSize / 8;
            var blockNumber = (streamPos / blockSizeInByte) + 1;
            var keyPos = streamPos % blockSizeInByte;

            //buffer
            var outBuffer = new byte[blockSizeInByte];
            var nonce = new byte[blockSizeInByte];
            var init = false;

            for (int i = offset; i < count; i++)
            {
                //encrypt the nonce to form next xor buffer (unique key)
                if (!init || (keyPos % blockSizeInByte) == 0)
                {
                    BitConverter.GetBytes(blockNumber).CopyTo(nonce, 0);
                    _encryptor.TransformBlock(nonce, 0, nonce.Length, outBuffer, 0);
                    if (init) keyPos = 0;
                    init = true;
                    blockNumber++;
                }
                buffer[i] ^= outBuffer[keyPos]; //simple XOR with generated unique key
                keyPos++;
            }
        }

        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;
        public override long Length => _stream.Length;
        public override long Position { get => _stream.Position; set => _stream.Position = value; }
        public override void Flush() => _stream.Flush();
        public override void SetLength(long value) => _stream.SetLength(value);
        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        public override int Read(byte[] buffer, int offset, int count)
        {
            var streamPos = this.Position;
            var ret = _stream.Read(buffer, offset, count);

            this.Cipher(buffer, offset, count, streamPos);

            return ret;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.Cipher(buffer, offset, count, this.Position);

            _stream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _encryptor?.Dispose();
                _aes?.Dispose();
                _stream?.Dispose();
            }

            base.Dispose(disposing);
        }


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
    }
}