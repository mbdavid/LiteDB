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
        private readonly bool _utcDate;

        private ArraySegment<byte> _current;
        private int _currentPosition = 0; // position in _current
        private int _position = 0; // global position

        private bool _isEOF = false;

        private byte[] _tempBuffer = new byte[16]; // re-usable array

        /// <summary>
        /// Current global cursor position
        /// </summary>
        public int Position => _position;

        /// <summary>
        /// Indicate position are at end of last source array segment
        /// </summary>
        public bool IsEOF => _isEOF;

        public BufferReader(IEnumerable<ArraySegment<byte>> source, bool utcDate = false)
        {
            _source = source.GetEnumerator();
            _utcDate = utcDate;

            _source.MoveNext();
            _current = _source.Current;
        }

        #region Basic Read

        /// <summary>
        /// Move fordward in current segment. If array segment finish, open next segment
        /// Returns true if move to another segment - returns false if continue in same segment
        /// </summary>
        private bool MoveFordward(int count)
        {
            // do not move fordward if source finish
            if (_isEOF) return false;

            //DEBUG
            if (_currentPosition + count > _current.Count) throw new InvalidOperationException("fordward are only for current segment");

            _currentPosition += count;
            _position += count;

            // request new source array if _current all consumed
            if (_currentPosition == _current.Count)
            {
                if (_source.MoveNext() == false)
                {
                    _isEOF = true;
                }
                else
                {
                    _current = _source.Current;
                    _currentPosition = 0;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Read bytes from source and copy into buffer. Return how many bytes was read
        /// </summary>
        public int Read(byte[] buffer, int offset, int count)
        {
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

                // move position in current segment (and go to next segment if finish)
                this.MoveFordward(bytesToCopy);

                if (_isEOF) break;
            }

            return bufferPosition;
        }

        /// <summary>
        /// Skip bytes (same as Read but with no array copy)
        /// </summary>
        public int Skip(int count) => this.Read(null, 0, count);

        #endregion

        #region Read String

        /// <summary>
        /// Read string with pre-fixed int32 length
        /// </summary>
        public string ReadString()
        {
            var count = this.ReadInt32();

            string value;

            // if fits in current segment, use inner array - otherwise copy from multiples segments
            if (_currentPosition + count <= _current.Count)
            {
                value = Encoding.UTF8.GetString(_current.Array, _current.Offset + _currentPosition, count);

                this.MoveFordward(count);
            }
            else
            {
                // rent a buffer to be re-usable
                var buffer = ArrayPool<byte>.Shared.Rent(count);

                this.Read(buffer, 0, count);

                value = Encoding.UTF8.GetString(buffer, 0, count);

                ArrayPool<byte>.Shared.Return(buffer);
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

                    this.MoveFordward(initialCount);

                    // and go to next segment
                    if (!_isEOF)
                    {
                        while (_current.Get(_currentPosition) != 0x00)
                        {
                            if (this.MoveFordward(1))
                            {
                                // write all segment into strem (did not found \0 yet)
                                mem.Write(_current.Array, _current.Offset, _current.Count);
                            }

                            if (_isEOF) break;
                        }

                        // add last segment (if eof already added in while)
                        if (!_isEOF)
                        {
                            mem.Write(_current.Array, _current.Offset, _currentPosition);
                        }

                        this.MoveFordward(1); // +1 to '\0'
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
                if (_current.Get(pos) == 0x00)
                {
                    value = Encoding.UTF8.GetString(_current.Array, _current.Offset + _currentPosition, count);

                    this.MoveFordward(count + 1); // +1 means '\0'

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

        #endregion

        #region Read Numbers

        private T ReadNumber<T>(Func<byte[], int, T> convert, int size)
        {
            T value;

            // if fits in current segment, use inner array - otherwise copy from multiples segments
            if (_currentPosition + size <= _current.Count)
            {
                value = convert(_current.Array, _current.Offset + _currentPosition);

                this.MoveFordward(size);
            }
            else
            {
                this.Read(_tempBuffer, 0, size);

                value = convert(_tempBuffer, 0);
            }

            return value;
        }

        public Int16 ReadInt16() => this.ReadNumber(BitConverter.ToInt16, 2);
        public Int32 ReadInt32() => this.ReadNumber(BitConverter.ToInt32, 4);
        public Int64 ReadInt64() => this.ReadNumber(BitConverter.ToInt64, 8);
        public UInt16 ReadUInt16() => this.ReadNumber(BitConverter.ToUInt16, 2);
        public UInt32 ReadUInt32() => this.ReadNumber(BitConverter.ToUInt32, 4);
        public UInt64 ReadUInt64() => this.ReadNumber(BitConverter.ToUInt64, 8);
        public Single ReadSingle() => this.ReadNumber(BitConverter.ToSingle, 4);
        public Double ReadDouble() => this.ReadNumber(BitConverter.ToDouble, 8);

        public Decimal ReadDecimal()
        {
            var a = this.ReadInt32();
            var b = this.ReadInt32();
            var c = this.ReadInt32();
            var d = this.ReadInt32();
            return new Decimal(new int[] { a, b, c, d });
        }

        #endregion

        public void Dispose()
        {
            _source.Dispose();
        }
    }
}