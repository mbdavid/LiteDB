using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Manage linear memory segments to avoid re-create array buffer in heap memory
    /// ThreadSafe
    /// </summary>
    internal class MemoryStore
    {
        private ConcurrentBag<ArraySegment<byte>> _store = new ConcurrentBag<ArraySegment<byte>>();

        public MemoryStore()
        {
            this.Extend();
        }

        public ArraySegment<byte> Rent(bool clear)
        {
            if (_store.TryTake(out var result))
            {
                if (clear)
                {
                    Array.Clear(result.Array, result.Offset, PAGE_SIZE);
                }

                return result;
            }
            else
            {
                this.Extend();

                return this.Rent(clear);
            }
        }

        public void Return(ref ArraySegment<byte> buffer)
        {
            _store.Add(buffer);
        }

        /// <summary>
        /// Clone source buffer to another new array segment. Use Rent() method to get new segment (must use "Return")
        /// This solution are faster than re-read data from disk
        /// </summary>
        public ArraySegment<byte> Clone(ArraySegment<byte> source)
        {
            var dest = this.Rent(false);

            // copy array bytes from source to dest using original array
            Buffer.BlockCopy(source.Array, source.Offset, dest.Array, dest.Offset, PAGE_SIZE);

            return dest;
        }

        /// <summary>
        /// Create new linar buffer (byte[]) in heap and get slices using ArraySegment. Each array segment contains one PAGE_SIZE
        /// </summary>
        private void Extend()
        {
            // lock store to ensure only 1 extend per time
            lock(_store)
            {
                if (_store.Count > 0) return;

                // create big linear array in heap
                var buffer = new byte[PAGE_SIZE * MEMORY_SEGMENT_SIZE];

                // slit linear array into many array segments
                for (var i = 0; i < MEMORY_SEGMENT_SIZE; i++)
                {
                    var segment = new ArraySegment<byte>(buffer, i * MEMORY_SEGMENT_SIZE, PAGE_SIZE);

                    _store.Add(segment);
                }
            }
        }
    }
}