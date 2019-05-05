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
    internal class MergeSortContainer : IDisposable
    {
        private readonly long _position;
        private readonly int _size;
        private readonly Stream _stream;
        private readonly bool _utcDate;

        private int _remaining = 0;
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

        public MergeSortContainer(long position, int size, Stream stream, bool utcDate)
        {
            _position = position;
            _size = size;
            _stream = stream;
            _utcDate = utcDate;
        }

        public void Store(IEnumerable<KeyValuePair<BsonValue, PageAddress>> items, int order, BufferSlice buffer)
        {
            var query = order == Query.Ascending ?
                items.OrderBy(x => x.Key) : items.OrderByDescending(x => x.Key);

            var offset = 0;

            foreach(var item in query)
            {
                ENSURE(item.Key.GetBytesCount(false) < 255, "sort key must be less than 255 bytes");

                buffer.WriteIndexKey(item.Key, offset);

                offset += item.Key.GetBytesCount(false) + 1 + (item.Key.IsString || item.Key.IsBinary ? 1 : 0); // +1 to DataType

                buffer.Write(item.Value, offset);

                offset += PageAddress.SIZE;

                _remaining++;
            }

            // store in disk
            _stream.Position = _position;
            _stream.Write(buffer.Array, 0, _size);

            // prepare reader
            _reader = new BufferReader(this.GetSource(), _utcDate);

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
        /// Get 8k page inside container file
        /// </summary>
        private IEnumerable<BufferSlice> GetSource()
        {
            var bytes = BufferPool.Rent(PAGE_SIZE);
            var buffer = new BufferSlice(bytes, 0, PAGE_SIZE);

            while (_readPosition < _size)
            {
                _stream.Position = _position + _readPosition;

                _stream.Read(bytes, 0, PAGE_SIZE);

                _readPosition += PAGE_SIZE;

                yield return buffer;
            }

            BufferPool.Return(bytes);
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }
    }
}