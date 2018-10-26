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

        /// <summary>
        /// Empty pages in store. If request page and has no more in store, do extend to create more memory allocation
        /// </summary>
        private readonly ConcurrentQueue<PageBuffer> _store = new ConcurrentQueue<PageBuffer>();

        /// <summary>
        /// Get how many extends was made in this store
        /// </summary>
        public int ExtendSegments { get; private set; } = 0;

        /// <summary>
        /// Count how many pages are avaiable to be reused inside _cleanPages
        /// </summary>
        private int _emptyShareCounter = 0;

        /// <summary>
        /// Control read/write store access locker
        /// </summary>
        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();

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
            var needIncrement = true;

            _locker.EnterReadLock();

            try
            {
                var page = _cleanPages.GetOrAdd(position, (pos) =>
                {
                    // get a clean page from store and read data from stream
                    var newPage = this.GetPageFromStore();

                    newPage.Position = position;

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
            finally
            {
                _locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Request for a writable page - no other can read this page and this page has no reference
        /// Writable pages can be write or just released (with no write)
        /// </summary>
        public PageBuffer GetWritablePage(long position, Action<long, ArraySlice<byte>> factory)
        {
            _locker.EnterReadLock();

            try
            {
                // write pages always contains a new buffer array
                var page = this.NewPage(position, false);

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
            finally
            {
                _locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Create new page using an empty buffer block. Mark this page as writable. Using reader locker
        /// </summary>
        public PageBuffer NewPage()
        {
            _locker.EnterReadLock();

            try
            {
                return this.NewPage(long.MaxValue, true);
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Create new page using an empty buffer block. Mark this page as writable
        /// </summary>
        private PageBuffer NewPage(long position, bool clear)
        {
            var page = this.GetPageFromStore();

            // clear page buffer
            page.Position = position;
            page.IsWritable = true;

            if (clear)
            {
                Array.Clear(page.Array, page.Offset, page.Count);
            }

            return page;
        }

        /// <summary>
        /// When page was requested as Writable and now changes are done
        /// Now, this page are clean and are considerd clean to be reused in _cleanPages
        /// </summary>
        public void MarkAsClean(PageBuffer page)
        {
            DEBUG(page.IsWritable == false, "only writable pages can be marked as clean");
            DEBUG(page.Position == long.MaxValue, "page position must be defined");
            DEBUG((_cleanPages.GetOrDefault(page.Position)?.ShareCounter ?? 1) == 0, "if page already in clean list MUST are not in use by any other thread to read");

            _locker.EnterReadLock();

            try
            {
                // add (or update) page in clean list
                _cleanPages[page.Position] = page;

                Interlocked.Increment(ref page.ShareCounter);
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        /// <summary>
        /// After use a page (for read/write) must return this page into store
        /// </summary>
        public void ReturnPage(PageBuffer page)
        {
            DEBUG(page.ShareCounter <= 0, "pages must contains share counter when return to store");

            _locker.EnterReadLock();

            try
            {
                // add or update 
                _cleanPages[page.Position] = page;

                // if on decrement share count this share will be zero, this page can be re-used
                if (Interlocked.Decrement(ref page.ShareCounter) == 0)
                {
                    // increment counter to know how many pages are unsued inside _cleanPages
                    Interlocked.Increment(ref _emptyShareCounter);

                    // must mark page as not writable (at this point, page already writed in disk)
                    page.IsWritable = false;
                }
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        private PageBuffer GetPageFromStore()
        {
            if (_store.TryDequeue(out var page))
            {
                page.ShareCounter = 1;

                DEBUG(page.IsWritable, "in memory store, page must be masked as non-writable");

                return page;
            }
            else
            {
                _locker.ExitReadLock();
                
                this.Extend();

                _locker.EnterReadLock();

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
            _locker.EnterWriteLock();

            try
            {
                if (_store.Count > 0) return;

                // if cleanPages contains more than 1 segment size of non shared counter, use this pages into store
                if (_emptyShareCounter > MEMORY_SEGMENT_SIZE)
                {
                    var pages = _cleanPages
                        .Values
                        .Where(x => x.ShareCounter == 0)
                        .Select(x => x.Position)
                        .ToArray();

                    DEBUG(pages.Length != _emptyShareCounter, "my counter must be same as ordinal count");

                    // remove pages from clean list and insert into store
                    foreach(var position in pages)
                    {
                        if (_cleanPages.TryRemove(position, out var page))
                        {
                            _store.Enqueue(page);
                        }
                    }

                    _emptyShareCounter = 0;
                }
                else
                {
                    // create big linear array in heap (G2 - > 85Kb)
                    var buffer = new byte[PAGE_SIZE * MEMORY_SEGMENT_SIZE];

                    this.ExtendSegments++;

                    // slit linear array into many array slices
                    for (var i = 0; i < MEMORY_SEGMENT_SIZE; i++)
                    {
                        _store.Enqueue(new PageBuffer(buffer, i * PAGE_SIZE));
                    }
                }
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        public void Dispose()
        {
        }
    }
}