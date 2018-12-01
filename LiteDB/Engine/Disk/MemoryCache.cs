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
    /// <summary>
    /// Manage linear memory segments to avoid re-create array buffer in heap memory
    /// Do not share same memory store with diferent files
    /// [ThreadSafe]
    /// </summary>
    internal class MemoryCache : IDisposable
    {
        /// <summary>
        /// Contains free ready-to-use pages in memory
        /// </summary>
        private readonly ConcurrentQueue<PageBuffer> _free = new ConcurrentQueue<PageBuffer>();

        /// <summary>
        /// Contains only clean pages (from both data/log file) - support page concurrency use
        /// </summary>
        private readonly ConcurrentDictionary<long, PageBuffer> _readable = new ConcurrentDictionary<long, PageBuffer>();

        /// <summary>
        /// Contains only writable (exclusive) pages. Can be loaded data from (data/log/new page) - no page concurrency
        /// </summary>
        private readonly ConcurrentDictionary<int, PageBuffer> _writable = new ConcurrentDictionary<int, PageBuffer>();

        /// <summary>
        /// Control read/write store access locker
        /// </summary>
        private readonly ReaderWriterLockSlim _locker;

        /// <summary>
        /// Get how many extends was made in this store
        /// </summary>
        private int _extends = 0;

        public MemoryCache(ReaderWriterLockSlim locker)
        {
            _locker = locker;

            this.Extend();
        }

        #region Readable Pages

        /// <summary>
        /// Get page from clean cache (readable). If page not exits, create this new page and load data using factory fn
        /// </summary>
        public PageBuffer GetReadablePage(long position, PageMode mode, Action<long, BufferSlice> factory)
        {
            _locker.EnterReadLock();

            try
            {
                // get dict key based on position/FileMode
                var key = this.GetReadableKey(position, mode);

                // try get from _readble dict or create new
                var page = _readable.GetOrAdd(key, (k) =>
                {
                    // get new page from _free pages (or extend)
                    var newPage = this.GetFreePage();

                    newPage.Position = position;
                    newPage.Mode = mode;

                    // load page content with disk stream
                    factory(position, newPage);

                    return newPage;
                });

                // update LRU
                Interlocked.Exchange(ref page.Timestamp, DateTime.UtcNow.Ticks);

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
        /// Get unique position in dictionary according DbFileMode. Use + or -
        /// </summary>
        private long GetReadableKey(long position, PageMode mode)
        {
            if (mode == PageMode.Data)
            {
                return position;
            }
            else
            {
                if (position == 0) return long.MinValue;
                return -position;
            }
        }

        #endregion

        #region Writable Pages

        /// <summary>
        /// Request for a writable page - no other can read this page and this page has no reference
        /// Writable pages can be write or just released (with no write)
        /// Before Dispose, a writable page must be marked as clean
        /// </summary>
        public PageBuffer GetWritablePage(long position, PageMode mode, Action<long, BufferSlice> factory)
        {
            _locker.EnterReadLock();

            try
            {
                // write pages always contains a new buffer array
                var writable = this.NewPage(position, mode, false);
                var key = this.GetReadableKey(position, mode);

                // if requested page already in cache, just copy buffer and avoid load from stream
                if (_readable.TryGetValue(key, out var clean))
                {
                    Buffer.BlockCopy(clean.Array, clean.Offset, writable.Array, writable.Offset, PAGE_SIZE);
                }
                else
                {
                    factory(position, writable);
                }

                return writable;
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Create new page using an empty buffer block. Mark this page as writable. Add into _writable list
        /// </summary>
        public PageBuffer NewPage()
        {
            _locker.EnterReadLock();

            try
            {
                return this.NewPage(long.MaxValue, PageMode.None, true);
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Create new page using an empty buffer block. Mark this page as writable. Add into _writable dict
        /// </summary>
        private PageBuffer NewPage(long position, PageMode mode, bool clear)
        {
            var page = this.GetFreePage();

            // set page position and page as writable
            page.Position = position;

            // define as writable
            page.ShareCounter = BUFFER_WRITABLE;

            // Timestamp = 0 means this page was never used (do not clear)
            if (clear && page.Timestamp > 0)
            {
                Array.Clear(page.Array, page.Offset, page.Count);
            }

            page.Mode = mode;

            // add into writable dict
            _writable.TryAdd(page.UniqueID, page);

            return page;
        }

        /// <summary>
        /// Try mode page from Writable list to readable list. If page already in _readable, keeps page in _writable
        /// </summary>
        public bool TryMoveToReadable(PageBuffer page)
        {
            ENSURE(page.Position != long.MaxValue, "Page must have a position");
            ENSURE(page.ShareCounter == BUFFER_WRITABLE, "Page must be writable");

            var key = this.GetReadableKey(page.Position, page.Mode);

            // set shareCounter to 1 (is in use) - there is no concurrency in write
            page.ShareCounter = 1;

            if(_readable.TryAdd(key, page))
            {
                // there is no need to this 2 operation be concurrent (add in _readable + remove in _writable)
                // because writable page has no concurrency
                _writable.TryRemove(page.UniqueID, out var dummy);

                return true;
            }
            else
            {
                // if page already in _readable, ok, keeps in _writable
                page.Timestamp = 1; // be first page to be re-used on next Extend()

                return false;
            }
        }

        /// <summary>
        /// Move page from Writable dict to Readable dict - if already exisits, override content
        /// Returns readable page
        /// </summary>
        public PageBuffer MoveToReadable(PageBuffer page)
        {
            ENSURE(page.ShareCounter == BUFFER_WRITABLE, "page must be writable before from to readable dict");
            ENSURE(_locker.IsReadLockHeld, "this method must be called inside a read locker");

            var key = this.GetReadableKey(page.Position, page.Mode);

            // no concurrency in writable page
            page.ShareCounter = 1;

            var readable = _readable.AddOrUpdate(key, page, (newKey, current) =>
            {
                ENSURE(current.ShareCounter == 0, "user must ensure this page are not in use when mark as read only");

                current.ShareCounter = 1;

                // if page already in cache, this is a duplicate page in memory
                // must update cached page with new page content
                Buffer.BlockCopy(page.Array, page.Offset, current.Array, current.Offset, PAGE_SIZE);

                return current;
            });

            return readable;
        }

        #endregion

        #region Cache managment

        /// <summary>
        /// Get a clean, re-usable page from store. If store are empty, can extend buffer segments
        /// </summary>
        private PageBuffer GetFreePage()
        {
            if (_free.TryDequeue(out var page))
            {
                ENSURE(page.Position == long.MaxValue, "pages in memory store must have no position defined");
                ENSURE(page.ShareCounter == 0, "pages in memory store must be non-shared");
                ENSURE(page.Mode == PageMode.None, "page in memory must have no page mode");

                return page;
            }
            // if no more page inside memory store - extend store/reuse non-shared pages
            else
            {
                _locker.ExitReadLock();

                this.Extend();

                _locker.EnterReadLock();

                return this.GetFreePage();
            }
        }

        /// <summary>
        /// Check if pages in cache (read/write) are not been used (ShareCounter = 0) and return to free list
        /// Otherwise, create new pages in memory and add into _free list
        /// </summary>
        private void Extend()
        {
            _locker.EnterWriteLock();

            try
            {
                // count how many pages in cache (read/write) are avaiable to be re-used
                var emptyShareCounter = 
                    _readable.Values.Count(x => x.ShareCounter == 0) +
                    _writable.Values.Count(x => x.ShareCounter == 0);

                // if this count are larger than MEMORY_SEGMENT_SIZE, re-use all this pages
                if (emptyShareCounter > MEMORY_SEGMENT_SIZE)
                {
                    // get all readable pages that can return to _free
                    var readables = _readable
                        .Where(x => x.Value.ShareCounter == 0)
                        .OrderBy(x => x.Value.Timestamp) // sort by timestamp to re-use oldest pages first
                        .Select(x => x.Key)
                        .Take(MEMORY_SEGMENT_SIZE)
                        .ToArray();

                    foreach (var key in readables)
                    {
                        if (_readable.TryRemove(key, out var page))
                        {
                            page.Position = long.MaxValue;
                            page.Mode = PageMode.None;

                            _free.Enqueue(page);
                        }
                    }

                    // now, do the same with writables pages
                    var writables = _writable
                        .Where(x => x.Value.ShareCounter == 0)
                        .OrderBy(x => x.Value.Timestamp) // sort by timestamp to re-use oldest pages first
                        .Select(x => x.Key)
                        .Take(MEMORY_SEGMENT_SIZE)
                        .ToArray();

                    foreach(var key in writables)
                    {
                        if (_writable.TryRemove(key, out var page))
                        {
                            page.Position = long.MaxValue;
                            page.Mode = PageMode.None;

                            _free.Enqueue(page);
                        }
                    }

                    LOG($"Re-using cache pages", "CACHE");
                }
                else
                {
                    // create big linear array in heap memory (G2 -> 85Kb)
                    var buffer = new byte[PAGE_SIZE * MEMORY_SEGMENT_SIZE];

                    // slit linear array into many array slices
                    for (var i = 0; i < MEMORY_SEGMENT_SIZE; i++)
                    {
                        var uniqueID = (_extends * MEMORY_SEGMENT_SIZE) + i;

                        _free.Enqueue(new PageBuffer(buffer, i * PAGE_SIZE, uniqueID));
                    }

                    _extends++;

                    LOG($"extending memory usage: (segments: {_extends} - used: {StorageUnitHelper.FormatFileSize(_extends * MEMORY_SEGMENT_SIZE * PAGE_SIZE)})", "CACHE");
                }
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        #endregion

        public void Dispose()
        {
        }
    }
}