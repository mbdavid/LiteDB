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
    /// Internal class that implement same idea from ArraySegment[byte] but use a class (not a struct). Works for byte[] only
    /// </summary>
    internal class BufferSlice
    {
        public int Offset { get; }
        public int Count { get; }
        public byte[] Array { get; }

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

        /// <summary>
        /// Fill all array slice with value parameter
        /// </summary>
        public void Fill(byte value)
        {
            this.Array.Fill(value, this.Offset, this.Count);
        }

        /// <summary>
        /// Checks if all values contains only value parameter (used for DEBUG)
        /// </summary>
        public bool All(byte value)
        {
            for (var i = 0; i < this.Count; i++)
            {
                if (this.Array[this.Offset + i] != value) return false;
            }

            return true;
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
            return $"Offset: {this.Offset} - Count: {this.Count}";
        }
    }
}