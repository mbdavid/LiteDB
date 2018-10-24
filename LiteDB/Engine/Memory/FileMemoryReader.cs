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
    /// 1 instance per thread - (NO thread safe)
    /// </summary>
    internal class FileMemoryReader : IDisposable
    {
        private readonly MemoryStore _memory;
        private readonly FileMemoryCache _cache;
        private readonly Stream _stream;
        private readonly Action<Stream> _dispose;

        private readonly List<PageBuffer> _pages = new List<PageBuffer>();

        public FileMemoryReader(MemoryStore memory, FileMemoryCache cache, Stream stream, Action<Stream> dispose)
        {
            _memory = memory;
            _cache = cache;
            _stream = stream;
            _dispose = dispose;
        }

        public PageBuffer GetPage(long position, bool readOnly)
        {
            return readOnly ? this.GetReadablePage(position) : this.GetWritablePage(position);
        }

        /// <summary>
        /// Get a PageBuffer from disk or cache. Ensure that page will be in cache during all use.
        /// </summary>
        private PageBuffer GetReadablePage(long position)
        {
            var isNew = false;

            // try get page from cache - otherwise read from disk
            var page = _cache.GetOrAddPage(position, (pos) =>
            {
                // rent 8k array buffer
                var buffer = _memory.Rent(false);

                _stream.Position = pos;

                // read page from disk
                _stream.Read(buffer.Array, buffer.Offset, PAGE_SIZE);

                isNew = true;

                // create new instance of page buffer with buffer slot
                return new PageBuffer
                {
                    Position = position,
                    ShareCounter = 1,
                    Buffer = buffer
                };
            });

            // if is not a new page, increment shared counter
            if (isNew == false)
            {
                // increment page share counter (will be decremented when reader dispose)
                Interlocked.Increment(ref page.ShareCounter);
            }

            // add page in local thread list
            _pages.Add(page);

            return page;
        }

        /// <summary>
        /// Get a clear/no re-used page buffer ready to be writable. Do not add into cache (can be changed)
        /// </summary>
        private PageBuffer GetWritablePage(long position)
        {
            ArraySegment<byte> buffer;

            // if page is in cache, get (avoiding disk read) but clone bytes
            if (_cache.TryGetPage(position, out var clean))
            {
                // get a buffer clone from cache (no reference)
                buffer = _memory.Clone(clean.Buffer);
            }
            else
            {
                // get buffer from memory store and read from stream
                buffer = _memory.Rent(false);

                _stream.Position = position;

                _stream.Read(buffer.Array, buffer.Offset, PAGE_SIZE);
            }

            // create new PageBuffer - so ShareCount must be 1
            var page = new PageBuffer
            {
                Position = position,
                ShareCounter = 1,
                Buffer = buffer
            };

            // adding new page into local list to be decremented when dispose
            _pages.Add(page);

            return page;
        }

        /// <summary>
        /// Create new page in memory to be used when engine need add page into datafile (log file first)
        /// </summary>
        public PageBuffer NewPage()
        {
            var buffer = _memory.Rent(true);

            var page = new PageBuffer
            {
                Position = long.MaxValue, // must be write only using "Append"
                ShareCounter = 1,
                Buffer = buffer
            };

            _pages.Add(page);

            return page;
        }

        /// <summary>
        /// Release all loaded pages that was loaded by this reader. Decrement page share counter
        /// If page get 0 share counter will be cleaner in next cleanup thread
        /// </summary>
        public void ReleasePages()
        {
            for (var i = 0; i < _pages.Count; i++)
            {
                var page = _pages[i];

                Interlocked.Decrement(ref page.ShareCounter);
            }

            _pages.Clear();
        }

        /// <summary>
        /// Decrement share-counter for all pages used in this reader
        /// All page that was write before reader dispose was incremented, so will note be clean after here
        /// </summary>
        public void Dispose()
        {
            this.ReleasePages();

            // return stream back to pool
            _dispose(_stream);
        }
    }
}