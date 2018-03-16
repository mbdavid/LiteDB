using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace LiteDB
{
    /// <summary>
    /// Implement and stream reader for chunk data (IEnumerable of byte array)
    /// </summary>
    public class ChunkStream : Stream
    {
        private long _length;
        private IEnumerator<byte[]> _source;

        private long _position;
        private byte[] _current = new byte[0];
        private int _currentPosition = 0;
        
        public ChunkStream(IEnumerable<byte[]> source, long length)
        {
            _length = length;
            _source = source.GetEnumerator();

            // initialize source
            _source.MoveNext();
            _current = _source.Current;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _length;

        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override void Flush() => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long count, SeekOrigin loc)
        {
            if (loc != SeekOrigin.Current || count < 0) throw new NotSupportedException("Fordward only");

            this.Read(null, 0, (int)count);

            return _position;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset != 0) throw new NotSupportedException("Offset must be zero");

            var length = _current.Length;
            var bufferPosition = 0;

            while(bufferPosition < count)
            {
                var bytesLeft = _current.Length - _currentPosition;
                var bytesToCopy = Math.Min(count - bufferPosition, bytesLeft);

                // fill buffer (if not null)
                if (buffer != null)
                {
                    Buffer.BlockCopy(_current, _currentPosition, buffer, bufferPosition, bytesToCopy);
                }

                bufferPosition += bytesToCopy;
                _currentPosition += bytesToCopy;

                // request new source array if _current all consumed
                if (_currentPosition == _current.Length)
                {
                    if (_source.MoveNext() == false) break;

                    _current = _source.Current;
                    _currentPosition = 0;
                }
            }

            return bufferPosition;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _source.Dispose();
        }
    }
}