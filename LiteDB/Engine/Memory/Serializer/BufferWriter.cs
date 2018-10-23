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

        public BufferWriter(IEnumerable<ArraySegment<byte>> source)
        {
            _source = source.GetEnumerator();

            _source.MoveNext();
            _current = _source.Current;
        }

        #region Basic Write


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
        /// Write bytes from buffer into segmentsr. Return how many bytes was write
        /// </summary>
        public int Write(byte[] buffer, int offset, int count)
        {
            var bufferPosition = 0;

            while (bufferPosition < count)
            {
                var bytesLeft = _current.Count - _currentPosition;
                var bytesToCopy = Math.Min(count - bufferPosition, bytesLeft);

                // fill buffer
                if (buffer != null)
                {
                    Buffer.BlockCopy(buffer, 
                        offset + bufferPosition,
                        _current.Array,
                        _current.Offset + _currentPosition, 
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
        /// Skip bytes (same as Write but with no array copy)
        /// </summary>
        public int Skip(int count) => this.Write(null, 0, count);

        #endregion

        #region Write String

        /// <summary>
        /// Write CString with \0 at end
        /// </summary>
        public void WriteCString(string value)
        {
            var bytesCount = Encoding.UTF8.GetByteCount(value);
            var available = _current.Count - _currentPosition; // avaiable in current segment

            // can write direct in current segment (use < because need +1 \0)
            if (bytesCount < available)
            {
                Encoding.UTF8.GetBytes(value, 0, value.Length, _current.Array, _current.Offset + _currentPosition);

                _current.Set(_currentPosition + bytesCount, 0x00);

                this.MoveFordward(bytesCount + 1); // +1 to '\0'
            }
            else
            {
                var buffer = ArrayPool<byte>.Shared.Rent(bytesCount);

                Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, 0);

                this.Write(buffer, 0, bytesCount);

                _current.Set(_currentPosition, 0x00);

                this.MoveFordward(1);

                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Write string pre-fixed with int32 bytes length
        /// </summary>
        public void WriteString(string value)
        {
            var count = Encoding.UTF8.GetByteCount(value);

            this.Write(count);

            if (count <= _current.Count)
            {
                Encoding.UTF8.GetBytes(value, 0, value.Length, _current.Array, _current.Offset + _currentPosition);

                this.MoveFordward(count);
            }
            else
            {
                // rent a buffer to be re-usable
                var buffer = ArrayPool<byte>.Shared.Rent(count);

                Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, 0);

                this.Write(buffer, 0, count);

                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        #endregion

        #region Numbers

        private void WriteNumber<T>(T value, Action<T, ArraySegment<byte>, int> toBytes, int size)
        {
            if (_currentPosition + size <= _current.Count)
            {
                toBytes(value, _current, _currentPosition);

                this.MoveFordward(size);
            }
            else
            {
                toBytes(value, _tempBuffer, 0);

                this.Write(_tempBuffer, 0, size);
            }
        }

        public void Write(Int16 value) => this.WriteNumber(value, BufferExtensions.ToBytes, 2);
        public void Write(Int32 value) => this.WriteNumber(value, BufferExtensions.ToBytes, 4);
        public void Write(Int64 value) => this.WriteNumber(value, BufferExtensions.ToBytes, 8);
        public void Write(UInt16 value) => this.WriteNumber(value, BufferExtensions.ToBytes, 2);
        public void Write(UInt32 value) => this.WriteNumber(value, BufferExtensions.ToBytes, 4);
        public void Write(UInt64 value) => this.WriteNumber(value, BufferExtensions.ToBytes, 8);
        public void Write(Single value) => this.WriteNumber(value, BufferExtensions.ToBytes, 4);
        public void Write(Double value) => this.WriteNumber(value, BufferExtensions.ToBytes, 8);

        public void Write(Decimal value)
        {
            var bits = Decimal.GetBits(value);
            this.Write(bits[0]);
            this.Write(bits[1]);
            this.Write(bits[2]);
            this.Write(bits[3]);
        }

        #endregion

        public void Dispose()
        {
            _source.Dispose();
        }
    }
}