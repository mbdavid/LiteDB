using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal class SortContainer : IDisposable
    {
        private readonly int _size;

        private int _remaining = 0;
        private int _count = 0;
        private bool _isEOF = false;

        private int _readPosition = 0;

        private BufferReader _reader = null;

        /// <summary>
        /// Returns if current container has no more items to read
        /// </summary>
        public bool IsEOF => _isEOF;

        /// <summary>
        /// Get current/last read value in container
        /// </summary>
        public KeyValuePair<BsonValue, PageAddress> Current;

        /// <summary>
        /// Get container disk position
        /// </summary>
        public long Position { get; set; } = -1;

        /// <summary>
        /// Get how many keyValues this container contains
        /// </summary>
        public int Count => _count;

        public SortContainer(int size)
        {
            _size = size;
        }

        public void Insert(IEnumerable<KeyValuePair<BsonValue, PageAddress>> items, int order, BufferSlice buffer)
        {
            var query = order == Query.Ascending ?
                items.OrderBy(x => x.Key) : items.OrderByDescending(x => x.Key);

            var offset = 0;

            foreach(var item in query)
            {
                buffer.WriteIndexKey(item.Key, offset);

                offset += GetKeyLength(item.Key);

                buffer.Write(item.Value, offset);

                offset += PageAddress.SIZE;

                _remaining++;
            }

            _count = _remaining;
        }

        /// <summary>
        /// Initialize reader based on Stream (if data was persisted in disk) or Buffer (if all data fit in only 1 container)
        /// </summary>
        public void InitializeReader(Stream stream, BufferSlice buffer, bool utcDate)
        {
            if (stream != null)
            {
                _reader = new BufferReader(this.GetSourceFromStream(stream), utcDate);
            }
            else
            {
                _reader = new BufferReader(buffer, utcDate);
            }

            this.MoveNext();
        }

        public bool MoveNext()
        {
            if (_remaining == 0)
            {
                _isEOF = true;
                return false;
            }

            var key = _reader.ReadIndexKey();
            var value = _reader.ReadPageAddress();

            this.Current = new KeyValuePair<BsonValue, PageAddress>(key, value);

            _remaining--;

            return true;
        }

        /// <summary>
        /// Get 8k buffer slices inside file container
        /// </summary>
        private IEnumerable<BufferSlice> GetSourceFromStream(Stream stream)
        {
            var bytes = BufferPool.Rent(PAGE_SIZE);
            var buffer = new BufferSlice(bytes, 0, PAGE_SIZE);

            while (_readPosition < _size)
            {
                stream.Position = this.Position + _readPosition;

                stream.Read(bytes, 0, PAGE_SIZE);

                _readPosition += PAGE_SIZE;

                yield return buffer;
            }

            BufferPool.Return(bytes);
        }

        public static int GetKeyLength(BsonValue key)
        {
            return 1 + // DataType
                key.GetBytesCount(false) + // BsonValue
                (key.IsString || key.IsBinary ? 1 : 0); // Key Length
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }
    }
}