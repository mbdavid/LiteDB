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
    /// Manage linear memory segments to avoid re-create array buffer in heap memory
    /// ThreadSafe
    /// </summary>
    internal class MemoryStore : IDisposable
    {
        /// <summary>
        /// Contains only clean-readonly pages indexed by position. Inside this collection pages can be in-use (SharedCounter > 0) or
        /// ready to be re-used.
        /// </summary>
        private readonly ConcurrentDictionary<long, PageBuffer> _cleanPages = new ConcurrentDictionary<long, PageBuffer>();

//        /// <summary>
//        /// Contains a temp list of all writable pages - this pages can be dirty (in change mode) or maybe clean - but pages
//        /// here are no sharable (ShareCounter must be 1). Only when mark as clean page will be remove from this list
//        /// </summary>
//        private readonly List<PageBuffer> _writablePages = new List<PageBuffer>();

        private readonly object _locker = new object();

        /// <summary>
        /// Empty pages in store. If request page and has no more in store, do extend to create more memory allocation
        /// </summary>
        private readonly ConcurrentQueue<PageBuffer> _store = new ConcurrentQueue<PageBuffer>();

        public MemoryStore()
        {
            this.Extend();
        }

        /// <summary>
        /// Request a readonly page - this page can be shared with all only for read (will never be dirty)
        /// Each request increase share counter
        /// </summary>
        public PageBuffer GetReadablePage(long position, Action<long, ArraySlice<byte>> factory)
        {
            //            DEBUG(_writablePages.Any(x => x.Position == position), "this page already in writable list");
            var needIncrement = true;

            var page = _cleanPages.GetOrAdd(position, (pos) =>
            {
                // get a clean page from store and read data from stream
                var newPage = this.GetPageFromStore();

                newPage.Position = position;
                newPage.ShareCounter = 1;

                needIncrement = false;

                factory(position, newPage);

                return newPage;
            });

            // increment only if page was already in _clean collection
            if (needIncrement)
            {
                Interlocked.Increment(ref page.ShareCounter);
            }

            return page;
        }

        /// <summary>
        /// Request for a writable page - no other can read this page and this page has no reference
        /// Writable pages can be write or just released (with no write)
        /// </summary>
        public PageBuffer GetWritablePage(long position, Action<long, ArraySlice<byte>> factory)
        {
//            DEBUG(_writablePages.Any(x => x.Position == position), "this page already in dirty list");

            // write pages always contains a new buffer array
            var page = this.NewPage(position, false);

            // ALERT !! mesmo pegando a pagina da colecao pode ser alterada entre o if e o buffer-copy

            // if request page already in clean list, just copy buffer and avoid load from stream
            if (_cleanPages.TryGetValue(position, out var clean))
            {
                Buffer.BlockCopy(clean.Array, clean.Offset, page.Array, page.Offset, PAGE_SIZE);
            }
            else
            {
                factory(position, page);
            }

            return page;
        }

        /// <summary>
        /// Create new page using an empty buffer block. Mark this page as writable
        /// </summary>
        public PageBuffer NewPage(long position, bool clear)
        {
            var page = this.GetPageFromStore();

            // clear page buffer
            page.Position = position;
            page.IsWritable = true;
            page.ShareCounter = 1;

            if (clear)
            {
                Array.Clear(page.Array, page.Offset, page.Count);
            }

//            _writablePages.Add(page);

            return page;
        }

        /// <summary>
        /// When page enter in queue to write, this page will be avaiable to read
        /// </summary>
        public void MarkAsClean(PageBuffer page)
        {
            DEBUG(page.IsWritable == false, "only writable pages can be marked as clean");
            DEBUG(page.Position == long.MaxValue, "page position must be defined");
            DEBUG((_cleanPages.GetOrDefault(page.Position)?.ShareCounter ?? 1) == 0, "if page already in clean list MUST are not in use by any other thread to read");

            // add (or update) page in clean list
            _cleanPages[page.Position] = page;

//            _writablePages.Remove(page);

            Interlocked.Increment(ref page.ShareCounter);
        }

        /// <summary>
        /// After use a page (for read/write) must return this page into store
        /// </summary>
        public void ReturnPage(PageBuffer page)
        {
            DEBUG(page.ShareCounter <= 0, "pages must contains share counter when return to store");

            // if on decrement share count this share will be zero, this page can be re-used
            if (Interlocked.Decrement(ref page.ShareCounter) == 0)
            {
                // must mark page as not writable (at this point, page already writed in disk)
                page.IsWritable = false;


                //
                // CLEAN-UP !! Must be in a sepate thread to run only when I want (memory usage)
                // 
                if (_cleanPages.TryRemove(page.Position, out var dummy))
                {
                    DEBUG(!Object.ReferenceEquals(page, dummy), "page instance must be same on clean collection");
                }

                _store.Enqueue(page);
            }
        }

        private PageBuffer GetPageFromStore()
        {
            if (_store.TryDequeue(out var page))
            {
                DEBUG(page.IsWritable, "in memory store, page must be masked as non-writable");

                return page;
            }
            else
            {
                this.Extend();

                return this.GetPageFromStore();
            }
        }

        /// <summary>
        /// Create new linar buffer (byte[]) in heap and get slices using PageBuffer (ArraySlice). Each array segment contains one PAGE_SIZE
        /// All buffer will be allocated into G2 because has more than 85k
        /// </summary>
        private void Extend()
        {
            // lock store to ensure only 1 extend per time
            lock(_locker)
            {
                if (_store.Count > 0) return;

                // create big linear array in heap (G2 - > 85Kb)
                var buffer = new byte[PAGE_SIZE * MEMORY_SEGMENT_SIZE];

                // slit linear array into many array slices
                for (var i = 0; i < MEMORY_SEGMENT_SIZE; i++)
                {
                    _store.Enqueue(new PageBuffer(buffer, i * PAGE_SIZE));
                }
            }
        }

        public void Dispose()
        {
        }
    }
}