using System;
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
    /// Internal class that implement same idea from ArraySegment but use a class (not a struct). Works for byte[] only
    /// </summary>
    public class BufferSlice
    {
        public readonly int Offset;
        public readonly int Count;
        public readonly byte[] Array;

        public BufferSlice(byte[] array, int offset, int count)
        {
            this.Array = array;
            this.Offset = offset;
            this.Count = count;
        }

        public byte this[int index]
        {
            get => this.Array[this.Offset + index];
            set => this.Array[this.Offset + index] = value;
        }

        public void Fill(byte value)
        {
            this.Array.Fill(value, this.Offset, this.Count);
        }

        /// <summary>
        /// Slice this buffer into new BufferSlice according new offset and new count
        /// </summary>
        public BufferSlice Slice(int offset, int count)
        {
            return new BufferSlice(this.Array, this.Offset + offset, count);
        }

        /// <summary>
        /// Convert this buffer slice into new byte[]
        /// </summary>
        public byte[] ToArray()
        {
            var buffer = new byte[this.Count];

            Buffer.BlockCopy(this.Array, this.Offset, buffer, 0, this.Count);

            return buffer;
        }

        public override string ToString()
        {
            return $"Count: {this.Count} - Offset: {this.Offset}";
        }

        #region Read/Write Shortcuts

        public void Write(Int32 value, int offset)
        {
            value.ToBytes(this.Array, this.Offset + offset);
        }

        public void Write(UInt32 value, int offset)
        {
            value.ToBytes(this.Array, this.Offset + offset);
        }

        public void Write(Int64 value, int offset)
        {
            value.ToBytes(this.Array, this.Offset + offset);
        }

        public void Write(DateTime value, int offset)
        {
            value.Ticks.ToBytes(this.Array, this.Offset + offset);
        }

        public void Write(string value, int offset, int count)
        {
            Encoding.UTF8.GetBytes(value, 0, count, this.Array, this.Offset + count);
        }

        public Int32 ReadInt32(int offset)
        {
            return BitConverter.ToInt32(this.Array, this.Offset + offset);
        }

        public UInt32 ReadUInt32(int offset)
        {
            return BitConverter.ToUInt32(this.Array, this.Offset + offset);
        }

        public Int64 ReadInt64(int offset)
        {
            return BitConverter.ToInt64(this.Array, this.Offset + offset);
        }

        public DateTime ReadDateTime(int offset)
        {
            return new DateTime(this.ReadInt64(offset), DateTimeKind.Utc).ToLocalTime();
        }

        public string ReadString(int offset, int count)
        {
            return Encoding.UTF8.GetString(this.Array, this.Offset + offset, count);
        }

        #endregion
    }
}