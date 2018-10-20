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

        private readonly List<PageBuffer> _pages;

        private readonly bool _utcDate;

        public FileMemoryReader(Action<Stream> dispose)
        {
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
            // try get page from cache - otherwise read from disk
            var page = _cache.GetOrAddPage(position, (pos) =>
            {
                // rent 8k array buffer
                var slot = _memory.Rent(false);

                _stream.Position = pos;

                // read page from disk
                _stream.Read(slot.Array, slot.Offset, PAGE_SIZE);

                // create new instance of page buffer with buffer slot
                return new PageBuffer
                {
                    Posistion = position,
                    ShareCounter = 0,
                    Buffer = slot
                };
            });

            // increment page reader (will remove from cache only when get 0 again: no one are using anymore)
            Interlocked.Increment(ref page.ShareCounter);

            // add page in local thread list
            _pages.Add(page);

            return page;
        }

        /// <summary>
        /// Get a clear/no re-used page buffer ready to be writable. Ensure no one more will se same data
        /// </summary>
        private PageBuffer GetWritablePage(long position)
        {
            ArraySegment<byte> buffer;

            // if page is in cache, get (avoiding disk read) but clone bytes
            if (_cache.TryGetPage(position, out var clean))
            {
                // get a buffer clone
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
                Posistion = position,
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
                Posistion = long.MaxValue, // must be write only using "Append"
                ShareCounter = 1,
                Buffer = buffer
            };

            _pages.Add(page);

            return page;
        }

        /// <summary>
        /// Decrement share-counter for all pages used in this reader
        /// </summary>
        public void Dispose()
        {
            for (var i = 0; i < _pages.Count; i++)
            {
                var page = _pages[i];

                Interlocked.Decrement(ref page.ShareCounter);
            }

            // return stream back to pool
            _dispose(_stream);
        }
    }
}