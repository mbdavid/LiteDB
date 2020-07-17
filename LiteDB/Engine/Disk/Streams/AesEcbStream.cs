using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Encrypted AES Stream using CipherMode = ECB (using 8K page block) - Used only for legacy 5.0.x encryption - All new 5.1.x will use XTS
    /// </summary>
    public class AesEcbStream : Stream
    {
        private readonly Aes _aes;
        private readonly ICryptoTransform _encryptor;
        private readonly ICryptoTransform _decryptor;

        private readonly Stream _stream;
        private readonly CryptoStream _reader;
        private readonly CryptoStream _writer;

        public AesEcbStream(string password, Stream stream, byte[] salt)
        {
            _stream = stream;

            _aes = Aes.Create();
            _aes.Padding = PaddingMode.None;
            _aes.Mode = CipherMode.ECB;

            using (var pdb = new Rfc2898DeriveBytes(password, salt))
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

        #region Stream implementations

        public override long Position { get => _stream.Position; set => _stream.Position = Position; }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override int Read(byte[] array, int offset, int count) => _reader.Read(array, offset, count);

        public override void Write(byte[] array, int offset, int count) => _writer.Write(array, offset, count);

        public override void Flush() => _stream.Flush();

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        public override void SetLength(long value) => _stream.SetLength(value);

        #endregion

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _stream.Dispose();

            _encryptor.Dispose();
            _decryptor.Dispose();

            _aes.Dispose();
        }
    }
}