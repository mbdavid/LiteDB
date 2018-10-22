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
    /// Read multiple array segment as a single linear segment - Fordward Only
    /// NO ThreadSafe
    /// </summary>
    public class BufferReader : IDisposable
    {
        private readonly IEnumerator<ArraySegment<byte>> _source;

        private ArraySegment<byte> _current;
        private int _currentPosition = 0; // position in _current
        private int _position = 0; // global position

        private byte[] _tempBuffer = new byte[256]; // re-usable array

        /// <summary>
        /// Current global cursor position
        /// </summary>
        public int Position => _position;

        public BufferReader(IEnumerable<ArraySegment<byte>> source)
        {
            _source = source.GetEnumerator();

            _source.MoveNext();
            _current = _source.Current;
        }

        /// <summary>
        /// Read bytes from source and copy into buffer. Return how many bytes was read
        /// </summary>
        public int Read(byte[] buffer, int offset, int count)
        {
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

            return bufferPosition;
        }

        /// <summary>
        /// Skip bytes (same as Read but with no array copy)
        /// </summary>
        public int Skip(int count) => this.Read(null, 0, count);

        /// <summary>
        /// Read string with fixed length
        /// </summary>
        public string ReadString(int count)
        {
            string value;

            // if fits in current segment, use inner array - otherwise copy from multiples segments
            if (_currentPosition + count <= _current.Count)
            {
                value = Encoding.UTF8.GetString(_current.Array, _current.Offset + _currentPosition, count);

                _currentPosition += count;
                _position += count;
            }
            else
            {
                // try use local temp buffer - if not fit, use ArrayPool shared buffer
                if (count < _tempBuffer.Length)
                {
                    this.Read(_tempBuffer, 0, count);

                    value = Encoding.UTF8.GetString(_tempBuffer, 0, count);
                }
                else
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(count);

                    this.Read(buffer, 0, count);

                    value = Encoding.UTF8.GetString(buffer, 0, count);

                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            return value;
        }

        /// <summary>
        /// Reading string until find \0 at end
        /// </summary>
        public string ReadCString()
        {
            if (this.TryReadCStringCurrentSegment(out var value))
            {
                return value;
            }
            else
            {
                using (var mem = new MemoryStream())
                {
                    // copy all first segment 
                    var initialCount = _current.Count - _currentPosition;

                    mem.Write(_current.Array, _current.Offset + _currentPosition, initialCount);

                    byte c;

                    // and go to next segment
                    if (_source.MoveNext())
                    {
                        _current = _source.Current;
                        _currentPosition = 0;

                        while ((c = _current[_currentPosition]) != 0x00)
                        {
                            mem.WriteByte(c);

                            _currentPosition += 1;
                            _position += 1;

                            if (_currentPosition == _current.Count)
                            {
                                if (_source.MoveNext() == false) break;

                                _current = _source.Current;
                                _currentPosition = 0;
                            }
                        }

                        _currentPosition += 1; // \0
                        _position += 1;
                    }

                    return Encoding.UTF8.GetString(mem.ToArray());
                }
            }
        }

        /// <summary>
        /// Try read CString in current segment avoind read byte-to-byte over segments
        /// </summary>
        private bool TryReadCStringCurrentSegment(out string value)
        {
            var pos = _currentPosition;
            var count = 0;

            while(pos < _current.Count)
            {
                if (_current[pos] == 0x00)
                {
                    value = Encoding.UTF8.GetString(_current.Array, _current.Offset + _currentPosition, count);

                    _currentPosition += count + 1; // + 1 means '\0'
                    _position += count + 1; // + 1 means '\0'

                    return true;
                }
                else
                {
                    count++;
                    pos++;
                }
            }

            value = null;

            return false;
        }


        /// <summary>
        /// Read next 4 bytes as Int32
        /// </summary>
        public int ReadInt32()
        {
            int value;

            // if fits in current segment, use inner array - otherwise copy from multiples segments
            if (_currentPosition + 4 <= _current.Count)
            {
                value = BitConverter.ToInt32(_current.Array, _current.Offset + _currentPosition);

                _currentPosition += 4;
                _position += 4;
            }
            else
            {
                // read 4 bytes from source
                this.Read(_tempBuffer, 0, 4);

                value = BitConverter.ToInt32(_tempBuffer, 0);
            }

            return value;
        }

        public void Dispose()
        {
            _source.Dispose();
        }
    }
}