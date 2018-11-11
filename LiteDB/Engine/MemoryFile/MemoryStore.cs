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
    /// Do not share same memory store with diferent files
    /// [ThreadSafe]
    /// </summary>
    internal class MemoryStore : IDisposable
    {
        private const int WRITABLE = -1;

        /// <summary>
        /// Cached pages contains only clean/readonly pages indexed by position. Inside this collection pages can be in-use (SharedCounter > 0) or
        /// ready to be re-used.
        /// </summary>
        private readonly ConcurrentDictionary<long, PageBuffer> _cache = new ConcurrentDictionary<long, PageBuffer>();

        /// <summary>
        /// Empty pages in store. If request page and has no more in store, do extend to create more memory allocation
        /// </summary>
        private readonly ConcurrentQueue<PageBuffer> _store = new ConcurrentQueue<PageBuffer>();

        /// <summary>
        /// Get how many extends was made in this store
        /// </summary>
        public int ExtendSegments { get; private set; } = 0;

        /// <summary>
        /// Control read/write store access locker
        /// </summary>
        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();

        public MemoryStore()
        {
            this.Extend();
        }

        /// <summary>
        /// Checks if some page are in cache (same instance) - DEBUG PROPROSE ONLY
        /// </summary>
        public bool InCache(PageBuffer page)
        {
            if (_cache.TryGetValue(page.Position, out var cached))
            {
                return page == cached;
            }
            return false;
        }

        /// <summary>
        /// Request a readonly page - this page can be shared with all only for read (will never be dirty)
        /// Each request increase share counter
        /// </summary>
        public PageBuffer GetReadablePage(long position, Action<long, ArraySlice<byte>> factory)
        {
            _locker.EnterReadLock();

            try
            {
                var page = _cache.GetOrAdd(position, (pos) =>
                {
                    // get a clean page from store and read data from stream
                    var newPage = this.GetPageFromStore();

                    newPage.Position = position;

                    factory(position, newPage);

                    return newPage;
                });

                // update LRU
                page.Timestamp = DateTime.UtcNow.Ticks;

                // increment share counter
                Interlocked.Increment(ref page.ShareCounter);

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
        /// Before release, a writable page must be marked as clean
        /// </summary>
        public PageBuffer GetWritablePage(long position, Action<long, ArraySlice<byte>> factory)
        {
            _locker.EnterReadLock();

            try
            {
                // write pages always contains a new buffer array
                var page = this.NewPage(position, false);

                // if requested page already in cache, just copy buffer and avoid load from stream
                if (_cache.TryGetValue(position, out var clean))
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

            // set page position and page as writable
            page.Position = position;

            // define as writable
            page.ShareCounter = WRITABLE;

            // Timestamp = 0 means this page was never used (do not clear)
            if (clear && page.Timestamp > 0)
            {
                Array.Clear(page.Array, page.Offset, page.Count);
            }

            return page;
        }

        /// <summary>
        /// Mark a writable page as read only page after made changes.
        /// If page is duplicated (already in clean list) update clean list and return page
        /// </summary>
        public PageBuffer MarkAsReadOnly(PageBuffer page, bool pageChanged)
        {
            DEBUG(page.ShareCounter != WRITABLE, "only writable pages can be marked as clean");
            DEBUG(page.Position == long.MaxValue, "to clean a page, position must be defined");

            _locker.EnterReadLock();

            try
            {
                // when I set shareCounter != -1 means this page are now clean page (no writes anymore)
                // (page instance as no concurrency)
                // if page will be added into writer queue (pageChaged) shared counter must be 1
                page.ShareCounter = pageChanged ? 1 : 0;

                // add in cache (return page inside collection)
                var result = _cache.AddOrUpdate(page.Position, page, (pos, current) =>
                {
                    DEBUG(current.ShareCounter != 0, "user must ensure this page are not in use when mark as read only");

                    // if page already in cache, this is a duplicate page in memory
                    // must update cached page with new page content
                    if (pageChanged)
                    {
                        Buffer.BlockCopy(page.Array, page.Offset, current.Array, current.Offset, PAGE_SIZE);
                    }

                    // and duplicated page now can return to store list
                    page.ShareCounter = 0;
                    page.Timestamp = 0;
                    page.Position = long.MaxValue;
                    _store.Enqueue(page);

                    // same current page will be inside page
                    return current;
                });

                // update LRU
                result.Timestamp = DateTime.UtcNow.Ticks;

                // result page must increment shared count - i can't just set to 1 because result page is concurrency page
                Interlocked.Increment(ref result.ShareCounter);

                return result;
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
            DEBUG(page.ShareCounter == 0, "page must be shared OR writable before return");

            // if page is writable it's mean that this page was requested to write BUT was not saved (was discard)
            if (page.ShareCounter == WRITABLE)
            {
                // update page instance with result page
                page = this.MarkAsReadOnly(page, false);

                DEBUG(page.ShareCounter >= 1, "after mark page as read only, share counter must return >= 1");
            }

            // now, decrement shareCounter
            Interlocked.Decrement(ref page.ShareCounter);
        }

        /// <summary>
        /// Get a clean, re-usable page from store. If store are empty, can extend buffer segments
        /// </summary>
        private PageBuffer GetPageFromStore()
        {
            if (_store.TryDequeue(out var page))
            {
                DEBUG(page.Position != long.MaxValue, "pages in memory store must have no position defined");
                DEBUG(page.ShareCounter != 0, "pages in memory store must be non-shared");

                return page;
            }
            // if no more page inside memory store - extend store/reuse non-shared pages
            else
            {
                DEBUG(_locker.IsReadLockHeld == false, "GetPageFromStore must be called inside read lock");

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
            _locker.EnterWriteLock();

            try
            {
                // count how many pages in cache are avaiable to be re-used
                var emptyShareCounter = _cache.Values
                    .Count(x => x.ShareCounter == 0);

                // if this count are larger than MEMORY_SEGMENT_SIZE, re-use all this pages
                if (emptyShareCounter > MEMORY_SEGMENT_SIZE)
                {
                    var pages = _cache
                        .Values
                        .Where(x => x.ShareCounter == 0)
                        .OrderBy(x => x.Timestamp) // sort by timestamp to re-use oldest pages first
                        .Select(x => x.Position)
                        .Take(MEMORY_SEGMENT_SIZE)
                        .ToArray();

                    // remove pages from clean list and insert into store
                    foreach (var position in pages)
                    {
                        if (_cache.TryRemove(position, out var page))
                        {
                            page.Position = long.MaxValue;

                            _store.Enqueue(page);
                        }
                    }
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
            //GC.SuppressFinalize(false);
        }
    }
}