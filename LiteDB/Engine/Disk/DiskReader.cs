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
    /// Memory file reader - must call Dipose after use to return reader into pool
    /// This class is not ThreadSafe - must have 1 instance per thread (get instance from DiskService)
    /// </summary>
    internal class DiskReader : IDisposable
    {
        private readonly MemoryCache _cache;

        private readonly StreamPool _streamPool;

        private readonly Lazy<Stream> _stream;

        public DiskReader(MemoryCache cache, StreamPool streamPool)
        {
            _cache = cache;
            _streamPool = streamPool;

            // use lazy because you can have transaction that will read only from cache
            _stream = new Lazy<Stream>(() => _streamPool.Rent());
        }

        public PageBuffer ReadPage(long position, bool writable)
        {
            ENSURE(position % PAGE_SIZE == 0, "invalid page position");

            var page = writable ?
                _cache.GetWritablePage(position, (pos, buf) => this.ReadStream(_stream.Value, pos, buf)) :
                _cache.GetReadablePage(position, (pos, buf) => this.ReadStream(_stream.Value, pos, buf));

            return page;
        }

        /// <summary>
        /// Read bytes from stream into buffer slice
        /// </summary>
        private void ReadStream(Stream stream, long position, BufferSlice buffer)
        {
            // can't test "Length" from out-to-date stream
            // ENSURE(stream.Length <= position - PAGE_SIZE, "can't be read from beyond file length");
            stream.Position = position;

            stream.Read(buffer.Array, buffer.Offset, buffer.Count);

            DEBUG(buffer.All(0) == false, "check if are not reading out of file length");
        }

        /// <summary>
        /// Request for a empty, writable non-linked page (same as DiskService.NewPage)
        /// </summary>
        public PageBuffer NewPage()
        {
            return _cache.NewPage();
        }

        /// <summary>
        /// When dispose, return stream to pool
        /// </summary>
        public void Dispose()
        {
            if (_stream.IsValueCreated)
            {
                _streamPool.Return(_stream.Value);
            }
        }
    }
}