using System;
using System.IO;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement internal thread-safe Stream using lock control - A single instance of ConcurrentStream are not multi thread,
    /// but multiples ConcurrentStream instances using same stream base will support concurrency
    /// </summary>
    public class ConcurrentStream : Stream
    {
        private Stream _stream;
        private long _position = 0;

        public ConcurrentStream(Stream stream)
        {
            _stream = stream;
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanWrite;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position { get => _position; set => _position = value; }

        public override void Flush() => _stream.Flush();

        public override void SetLength(long value) => _stream.SetLength(value);

        protected override void Dispose(bool disposing) => _stream.Dispose();

        public override long Seek(long offset, SeekOrigin origin)
        {
            lock(_stream)
            {
                var position =
                    origin == SeekOrigin.Begin ? offset :
                    origin == SeekOrigin.Current ? _position + offset :
                    _position - offset;

                _position = position;

                return _position;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // lock internal stream and set position before read
            lock (_stream)
            {
                _stream.Position = _position;
                var read = _stream.Read(buffer, offset, count);
                _position = _stream.Position;
                return read;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // lock internal stream and set position before write
            lock (_stream)
            {
                _stream.Position = _position;
                _stream.Write(buffer, offset, count);
                _position = _stream.Position;
            }
        }
    }
}