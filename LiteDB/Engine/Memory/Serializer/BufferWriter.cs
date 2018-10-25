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
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Write data into multiple array segment
    /// NO ThreadSafe
    /// </summary>
    public class BufferWriter : IDisposable
    {
        private readonly IEnumerator<ArraySlice<byte>> _source;

        private ArraySlice<byte> _current;
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

        public BufferWriter(IEnumerable<ArraySlice<byte>> source)
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
            DEBUG(_currentPosition + count > _current.Count, "fordward are only for current segment");

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
        /// Write bytes from buffer into segmentsr. Return how many bytes was write
        /// </summary>
        public int Write(byte[] buffer) => this.Write(buffer, 0, buffer.Length);

        /// <summary>
        /// Skip bytes (same as Write but with no array copy)
        /// </summary>
        public int Skip(int count) => this.Write(null, 0, count);

        #endregion

        #region String

        /// <summary>
        /// Write String with \0 at end
        /// </summary>
        public void WriteCString(string value)
        {
            var bytesCount = Encoding.UTF8.GetByteCount(value);
            var available = _current.Count - _currentPosition; // avaiable in current segment

            // can write direct in current segment (use < because need +1 \0)
            if (bytesCount < available)
            {
                Encoding.UTF8.GetBytes(value, 0, value.Length, _current.Array, _current.Offset + _currentPosition);

                _current[_currentPosition + bytesCount] = 0x00;

                this.MoveFordward(bytesCount + 1); // +1 to '\0'
            }
            else
            {
                var buffer = ArrayPool<byte>.Shared.Rent(bytesCount);

                Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, 0);

                this.Write(buffer, 0, bytesCount);

                _current[_currentPosition] = 0x00;

                this.MoveFordward(1);

                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Write string into output buffer. 
        /// Support direct string (with no length information) or BSON specs (with Legnth + 1 before and \0 at end)
        /// </summary>
        public void WriteString(string value, bool specs)
        {
            var count = Encoding.UTF8.GetByteCount(value);

            if (specs)
            {
                this.Write(count + 1); // write Length + 1 (for \0)
            }

            if (count <= _current.Count - _currentPosition)
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

            if (specs)
            {
                this.Write((byte)0x00);
            }
        }

        #endregion

        #region Numbers

        private void WriteNumber<T>(T value, Action<T, byte[], int> toBytes, int size)
        {
            if (_currentPosition + size <= _current.Count)
            {
                toBytes(value, _current.Array, _current.Offset + _currentPosition);

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

        #region Complex Types

        /// <summary>
        /// Write DateTime as UTC ticks (not BSON format)
        /// </summary>
        public void Write(DateTime value)
        {
            this.Write(value.ToUniversalTime().Ticks);
        }

        /// <summary>
        /// Write Guid as 16 bytes array
        /// </summary>
        public void Write(Guid value)
        {
            // there is no avaiable value.TryWriteBytes (TODO: implement conditional compile)?
            var bytes = value.ToByteArray();

            this.Write(bytes, 0, 16);
        }

        /// <summary>
        /// Write ObjectId as 12 bytes array
        /// </summary>
        public void Write(ObjectId value)
        {
            if (_currentPosition + 12 <= _current.Count)
            {
                value.ToByteArray(_current.Array, _current.Offset + _currentPosition);

                this.MoveFordward(12);
            }
            else
            {
                value.ToByteArray(_tempBuffer, 0);

                this.Write(_tempBuffer, 0, 12);
            }
        }

        /// <summary>
        /// Write a boolean as 1 byte (0 or 1)
        /// </summary>
        public void Write(bool value)
        {
            _current[_currentPosition] = value ? (byte)0x00 : (byte)0x01;
            this.MoveFordward(1);
        }

        /// <summary>
        /// Write single byte
        /// </summary>
        public void Write(byte value)
        {
            _current[_currentPosition] = value;
            this.MoveFordward(1);
        }

        /// <summary>
        /// Write PageAddress as PageID, Index
        /// </summary>
        internal void Write(PageAddress address)
        {
            this.Write(address.PageID);
            this.Write(address.Index);
        }

        #endregion

        #region BsonValue for IndexKey

        /// <summary>
        /// Write a BSON value into output. Do not respect BSON document specs, becase write single value
        /// Do not store length for variable types (byte[] or string) - must know when read
        /// If value is BsonArray or BsonDocument will write full BSON spcecs (using Elements)
        /// Used ONLY Index Key storage
        /// </summary>
        public void WriteBsonValue(BsonValue value)
        {
            this.Write((byte)value.Type);

            switch (value.Type)
            {
                case BsonType.Null:
                case BsonType.MinValue:
                case BsonType.MaxValue:
                    break;

                case BsonType.Int32: this.Write((Int32)value.RawValue); break;
                case BsonType.Int64: this.Write((Int64)value.RawValue); break;
                case BsonType.Double: this.Write((Double)value.RawValue); break;
                case BsonType.Decimal: this.Write((Decimal)value.RawValue); break;

                case BsonType.String: this.WriteString((String)value.RawValue, false); break;

                case BsonType.Document: this.WriteDocument(value.AsDocument); break;
                case BsonType.Array: this.WriteArray(value.AsArray); break;

                case BsonType.Binary: this.Write((Byte[])value.RawValue); break;
                case BsonType.ObjectId: this.Write((ObjectId)value.RawValue); break;
                case BsonType.Guid: this.Write((Guid)value.RawValue); break;

                case BsonType.Boolean: this.Write((Boolean)value.RawValue); break;
                case BsonType.DateTime: this.Write((DateTime)value.RawValue); break;

                default: throw new NotImplementedException();
            }
        }

        #endregion

        #region BsonDocument as SPECS

        /// <summary>
        /// Write BsonArray as BSON specs
        /// </summary>
        public void WriteArray(BsonArray value)
        {
            this.Write(value.GetBytesCount(false));

            for (var i = 0; i < value.Count; i++)
            {
                this.WriteElement(i.ToString(), value[i] ?? BsonValue.Null);
            }

            this.Write((byte)0x00);
        }

        /// <summary>
        /// Write BsonDocument as BSON specs
        /// </summary>
        public void WriteDocument(BsonDocument value)
        {
            this.Write(value.GetBytesCount(false));

            foreach (var key in value.Keys)
            {
                this.WriteElement(key, value[key] ?? BsonValue.Null);
            }

            this.Write((byte)0x00);
        }

        private void WriteElement(string key, BsonValue value)
        {
            // cast RawValue to avoid one if on As<Type>
            switch (value.Type)
            {
                case BsonType.Double:
                    this.Write((byte)0x01);
                    this.WriteCString(key);
                    this.Write((Double)value.RawValue);
                    break;

                case BsonType.String:
                    this.Write((byte)0x02);
                    this.WriteCString(key);
                    this.WriteString((String)value.RawValue, true); // true = BSON Specs (add LENGTH at begin + \0 at end)
                    break;

                case BsonType.Document:
                    this.Write((byte)0x03);
                    this.WriteCString(key);
                    this.WriteDocument(new BsonDocument((Dictionary<string, BsonValue>)value.RawValue));
                    break;

                case BsonType.Array:
                    this.Write((byte)0x04);
                    this.WriteCString(key);
                    this.WriteArray(new BsonArray((List<BsonValue>)value.RawValue));
                    break;

                case BsonType.Binary:
                    this.Write((byte)0x05);
                    this.WriteCString(key);
                    var bytes = (byte[])value.RawValue;
                    this.Write(bytes.Length);
                    this.Write((byte)0x00); // subtype 00 - Generic binary subtype
                    this.Write(bytes, 0, bytes.Length);
                    break;

                case BsonType.Guid:
                    this.Write((byte)0x05);
                    this.WriteCString(key);
                    var guid = (Guid)value.RawValue;
                    this.Write(16);
                    this.Write((byte)0x04); // UUID
                    this.Write(guid);
                    break;

                case BsonType.ObjectId:
                    this.Write((byte)0x07);
                    this.WriteCString(key);
                    this.Write((ObjectId)value.RawValue);
                    break;

                case BsonType.Boolean:
                    this.Write((byte)0x08);
                    this.WriteCString(key);
                    this.Write((byte)(((Boolean)value.RawValue) ? 0x01 : 0x00));
                    break;

                case BsonType.DateTime:
                    this.Write((byte)0x09);
                    this.WriteCString(key);
                    var date = (DateTime)value.RawValue;
                    // do not convert to UTC min/max date values - #19
                    var utc = (date == DateTime.MinValue || date == DateTime.MaxValue) ? date : date.ToUniversalTime();
                    var ts = utc - BsonValue.UnixEpoch;
                    this.Write(Convert.ToInt64(ts.TotalMilliseconds));
                    break;

                case BsonType.Null:
                    this.Write((byte)0x0A);
                    this.WriteCString(key);
                    break;

                case BsonType.Int32:
                    this.Write((byte)0x10);
                    this.WriteCString(key);
                    this.Write((Int32)value.RawValue);
                    break;

                case BsonType.Int64:
                    this.Write((byte)0x12);
                    this.WriteCString(key);
                    this.Write((Int64)value.RawValue);
                    break;

                case BsonType.Decimal:
                    this.Write((byte)0x13);
                    this.WriteCString(key);
                    this.Write((Decimal)value.RawValue);
                    break;

                case BsonType.MinValue:
                    this.Write((byte)0xFF);
                    this.WriteCString(key);
                    break;

                case BsonType.MaxValue:
                    this.Write((byte)0x7F);
                    this.WriteCString(key);
                    break;
            }
        }

        #endregion

        public void Dispose()
        {
            _source.Dispose();
        }
    }
}