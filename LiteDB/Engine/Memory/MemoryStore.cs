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
    public class MemoryStore
    {
        private ConcurrentBag<ArraySegment<byte>> _store = new ConcurrentBag<ArraySegment<byte>>();

        public MemoryStore()
        {
            this.Extend();
        }

        public ArraySegment<byte> Rent()
        {
            if (_store.TryTake(out var result))
            {
                return result;
            }
            else
            {
                this.Extend();

                return this.Rent();
            }
        }

        public void Return(ArraySegment<byte> buffer)
        {
            _store.Add(buffer);
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

                for (var i = 0; i < MEMORY_SEGMENT_SIZE; i++)
                {
                    var segment = new ArraySegment<byte>(buffer, i * MEMORY_SEGMENT_SIZE, PAGE_SIZE);

                    _store.Add(segment);
                }
            }
        }
    }
}