using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace LiteDB.Engine
{
    /// <summary>
    /// Write data into multiple array segment
    /// NO ThreadSafe
    /// </summary>
    public class BufferWriter : IDisposable
    {
        private readonly IEnumerator<ArraySegment<byte>> _source;

        private ArraySegment<byte> _current;
        private int _currentPosition = 0; // position in _current
        private int _position = 0; // global position

        private byte[] _tempBuffer = new byte[256]; // re-usable array

        public int Position => _position;

        public BufferWriter(IEnumerable<ArraySegment<byte>> source)
        {
            _source = source.GetEnumerator();

            _source.MoveNext();
            _current = _source.Current;
        }

        /// <summary>
        /// Read bytes from source and copy into buffer. Return how many bytes was read
        /// </summary>
        public int Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
            /*
            var length = _current.Count;
            var bufferPosition = 0;

            while (bufferPosition < count)
            {
                var bytesLeft = _current.Count - _currentPosition;
                var bytesToCopy = Math.Min(count - bufferPosition, bytesLeft);

                // fill buffer
                if (buffer != null)
                {
                    Buffer.BlockCopy(_current.Array, 
                        _current.Offset + _currentPosition, 
                        buffer, 
                        offset + bufferPosition, 
                        bytesToCopy);
                }

                bufferPosition += bytesToCopy;
                _currentPosition += bytesToCopy;
                _position += bytesToCopy;

                // request new source array if _current all consumed
                if (_currentPosition == _current.Count)
                {
                    if (_source.MoveNext() == false) break;

                    _current = _source.Current;
                    _currentPosition = 0;
                }
            }

            return bufferPosition;*/
        }

        /// <summary>
        /// Skip bytes (same as Write but with no array copy)
        /// </summary>
        public int Skip(int count) => this.Write(null, 0, count);

        /// <summary>
        /// Write CString with \0 at end
        /// </summary>
        public void Write(string value)
        {
            var bytesCount = Encoding.UTF8.GetByteCount(value);
            var available = _current.Count - _currentPosition;

            // can write direct in current segment (use < because need +1 \0)
            if (bytesCount < available)
            {
                Encoding.UTF8.GetBytes(value, 0, value.Length, _current.Array, _current.Offset + _currentPosition);

                _current[_currentPosition + bytesCount] = 0x00;

                _currentPosition += bytesCount + 1;
                _position += bytesCount + 1;
            }
            else
            {
                if (bytesCount < _tempBuffer.Length)
                {
                    Encoding.UTF8.GetBytes(value, 0, value.Length, _tempBuffer, 0);

                    this.Write(_tempBuffer, 0, bytesCount);
                }
                else
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(bytesCount);

                    Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, 0);

                    this.Write(buffer, 0, bytesCount);

                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        public void Dispose()
        {
            _source.Dispose();
        }
    }
}