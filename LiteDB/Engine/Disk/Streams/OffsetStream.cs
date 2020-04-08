using System;
using System.IO;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement a internal stream based in another stream with a length offset
    /// </summary>
    internal class OffsetStream : Stream
    {
        private readonly Stream _stream;
        private readonly long _offset;

        public OffsetStream(Stream stream, long offset)
        {
            _stream = stream;
            _offset = offset;

            _stream.Position = offset;
        }

        public Stream BaseStream => _stream;

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length - _offset;

        public override long Position { get => _stream.Position - _offset; set => _stream.Position = value + _offset; }

        public override void Flush() => _stream.Flush();

        public override void SetLength(long value) => _stream.SetLength(value + _offset);

        protected override void Dispose(bool disposing) => _stream.Dispose();

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset + _offset, origin);

        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

        public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);

    }
}