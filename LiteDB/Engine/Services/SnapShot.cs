using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represent a single snapshot
    /// </summary>
    internal class Snapshot : IDisposable
    {
        // instances from Engine
        private readonly HeaderPage _header;
        private readonly LockService _locker;
        private readonly DiskReader _reader;
        private readonly WalIndexService _walIndex;

        // instances from transaction
        private readonly TransactionPages _transPages;

        // snapshot controls
        private readonly int _readVersion;
        private readonly LockMode _mode;
        private readonly string _collectionName;
        private readonly CollectionPage _collectionPage;

        // local page cache - contains only pages about this collection (but do not contains CollectionPage - use this.CollectionPage)
        private readonly Dictionary<uint, BasePage> _localPages = new Dictionary<uint, BasePage>();

        // expose
        public LockMode Mode => _mode;
        public string CollectionName => _collectionName;
        public CollectionPage CollectionPage => _collectionPage;

        public Snapshot(LockMode mode, string collectionName, HeaderPage header, TransactionPages transPages, LockService locker, WalIndexService walIndex, DiskReader reader, bool addIfNotExists)
        {
            _mode = mode;
            _collectionName = collectionName;
            _header = header;
            _transPages = transPages;
            _locker = locker;
            _walIndex = walIndex;
            _reader = reader;

            // enter in lock mode according initial mode
            if (mode == LockMode.Read)
            {
                _locker.EnterRead(_collectionName);
            }
            else
            {
                _locker.EnterReserved(_collectionName);
            }

            // get lastest read version from wal-index
            _readVersion = _walIndex.CurrentReadVersion;

            var srv = new CollectionService(_header, this, _transPages);

            // read collection (create if new - load virtual too)
            srv.Get(_collectionName, addIfNotExists, ref _collectionPage);
        }

        /// <summary>
        /// Return all dirty pages (can include collection)
        /// </summary>
        public IEnumerable<BasePage> GetDirtyPages(bool includeCollectionPage)
        {
            // if snapshot is read only, just exit
            if (_mode == LockMode.Read) yield break;

            foreach(var page in _localPages.Values.Where(x => x.IsDirty))
            {
                ENSURE(page.PageType != PageType.Header && page.PageType != PageType.Collection, "local cache cann't contains this page type");

                yield return page;
            }

            if (includeCollectionPage && _collectionPage != null && _collectionPage.IsDirty)
            {
                yield return _collectionPage;
            }
        }

        /// <summary>
        /// Get all clean pages (not modified)
        /// </summary>
        public IEnumerable<BasePage> GetCleanPages()
        {
            return _localPages.Values.Where(x => x.IsDirty == false);
        }

        /// <summary>
        /// Clear all local pages and return page buffer to file reader
        /// </summary>
        public void Clear()
        {
            // release all pages (except collection page)
            foreach(var page in _localPages.Values)
            {
                page.GetBuffer(false).Release();
            }

            _localPages.Clear();
        }

        /// <summary>
        /// Dispose stream readers and exit collection lock
        /// </summary>
        public void Dispose()
        {
            // release collection page
            _collectionPage?.GetBuffer(false).Release();

            if (_mode == LockMode.Read)
            {
                _locker.ExitRead(_collectionName);
            }
            else if(_mode == LockMode.Write)
            {
                _locker.ExitReserved(_collectionName);
            }
        }

        #region Page Version functions

        /// <summary>
        /// Get a a valid page for this snapshot (must consider local-index and wal-index)
        /// </summary>
        public T GetPage<T>(uint pageID)
            where T : BasePage
        {
            ENSURE(pageID != 0, "never should request header page (always use global _header instance)");

            // first, look for this page inside local pages
            if (_localPages.TryGetValue(pageID, out var page))
            {
                return (T)page;
            }

            // if page is not in local cache, get from disk (log/wal/data)
            page = this.ReadPage<T>(pageID);

            // store now in local cache (except collection page)
            if (page.PageType != PageType.Collection)
            {
                // add into local pages
                _localPages[pageID] = page;

                // increment transaction size counter
                _transPages.TransactionSize++;
            }

            return (T)page;
        }

        /// <summary>
        /// Read page from disk (dirty, wal or data)
        /// </summary>
        private T ReadPage<T>(uint pageID)
            where T : BasePage
        {
            // if not inside local pages can be a dirty page saved in log file
            if (_transPages.DirtyPages.TryGetValue(pageID, out var position))
            {
                // read page from log file
                var buffer = _reader.ReadPage(position.Position, _mode == LockMode.Write, FileOrigin.Log);
                var dirty = BasePage.ReadPage<T>(buffer);

                return dirty;
            }

            // now, look inside wal-index
            var pos = _walIndex.GetPageIndex(pageID, _readVersion);

            if (pos != long.MaxValue)
            {
                // read page from log file
                var buffer = _reader.ReadPage(pos, _mode == LockMode.Write, FileOrigin.Log);
                var logPage = BasePage.ReadPage<T>(buffer);

                // clear some data inside this page (will be override when write on log file)
                logPage.TransactionID = 0;
                logPage.IsConfirmed = false;

                return logPage;
            }
            else
            {
                ENSURE(pageID <= _header.LastPageID, "request page must be less or equals lastest page in data file");

                // for last chance, look inside original disk data file
                var pagePosition = BasePage.GetPagePosition(pageID);

                // read page from data file
                var buffer = _reader.ReadPage(position.Position, _mode == LockMode.Write, FileOrigin.Data);
                var diskpage = BasePage.ReadPage<T>(buffer);

                return diskpage;
            }
        }

        /// <summary>
        /// Read all sequences pages from a start pageID (using NextPageID)
        /// </summary>
        public IEnumerable<T> GetSeqPages<T>(uint firstPageID)
            where T : BasePage
        {
            var pageID = firstPageID;

            while (pageID != uint.MaxValue)
            {
                var page = this.GetPage<T>(pageID);

                pageID = page.NextPageID;

                yield return page;
            }
        }

        /// <summary>
        /// Get a new empty page - can be a reused page (EmptyPage) or a clean one (extend datafile)
        /// FreeEmptyPageID use a single linked list to avoid read old pages. Insert/Delete pages from this list is always from begin
        /// </summary>
        public T NewPage<T>()
            where T : BasePage
        {
            var pageID = 0u;
            PageBuffer buffer;

            // do not release collection page (only at dispose)
            var release = typeof(T) != typeof(CollectionPage);

            // lock header instance to get new page
            lock (_header)
            {
                // try get page from Empty free list
                if (_header.FreeEmptyPageID != uint.MaxValue)
                {
                    var free = this.GetPage<BasePage>(_header.FreeEmptyPageID);

                    ENSURE(free.PageType == PageType.Empty, "empty page must be defined as empty type");

                    // set header free empty page to next free page
                    _header.FreeEmptyPageID = free.NextPageID;

                    // get pageID from empty list
                    pageID = free.PageID;

                    // get buffer inside re-used page
                    buffer = free.GetBuffer(false);
                }
                else
                {
                    // increase LastPageID from shared page
                    pageID = ++_header.LastPageID;

                    // request for a new buffer
                    buffer = _reader.NewPage();
                }
            }

            var page = BasePage.CreatePage<T>(buffer, pageID);

            // update local cache with new instance T page type
            if (page.PageType != PageType.Collection)
            {
                _localPages[pageID] = page;
            }

            // define ColID for this new page (if this.CollectionPage is null, so this is new collection page)
            page.ColID = _collectionPage?.PageID ?? page.PageID;

            // define as dirty to override pageType
            page.IsDirty = true;

            // retain a list of created pages to, in a rollback situation, back pages to empty list
            _transPages.NewPages.Add(pageID);

            // increment transaction size
            _transPages.TransactionSize++;

            return page;
        }

        /// <summary>
        /// Delete all sequence of pages based on first pageID
        /// </summary>
        public void DeletePages(uint firstPageID)
        {
            // get all pages in sequence or a single one
            var pages = this.GetSeqPages<BasePage>(firstPageID);

            foreach (var page in pages)
            {
                this.DeletePage(page);
            }
        }

        /// <summary>
        /// Delete a page - transform them in Empty Page and add to EmptyPageList
        /// </summary>
        public void DeletePage(BasePage page)
        {
            // if first page in sequence
            if (_transPages.FirstDeletedPageID == uint.MaxValue)
            {
                // set first and last deleted page as current deleted page
                _transPages.FirstDeletedPageID = page.PageID;
                _transPages.LastDeletedPageID = page.PageID;
            }
            else
            {
                // set next link from current deleted page to first deleted page
                page.NextPageID = _transPages.FirstDeletedPageID;

                // and then, set this current deleted page as first page making a linked list
                _transPages.FirstDeletedPageID = page.PageID;
            }

            // remove from linked-list
            ENSURE(page.PrevPageID == uint.MaxValue && page.NextPageID == uint.MaxValue, "before delete a page, no linked list with any another page");

            // define page as empty
            page.PageType = PageType.Empty;
            page.IsDirty = true;

            // there is no need update _localPages because page instance still same

            _transPages.DeletedPages++;
        }

        /// <summary>
        /// Returns a page that contains space enough to data to insert new object - if one does not exit, creates a new page.
        /// Before return page, fix empty free list slot according with passed length
        /// </summary>
        public T GetFreePage<T>(int bytesLength)
            where T : BasePage
        {
            // get length in blocks
            var length = (byte)((bytesLength / PAGE_BLOCK_SIZE) + 1);

            // select if I will get from free index list or data list
            var freeLists = typeof(T) == typeof(IndexPage) ?
                _collectionPage.FreeIndexPageID :
                _collectionPage.FreeDataPageID;

            // do not consider last slot (keep this as PCT_FREE) pages with less than 76 blocks will no use for new data
            var startSlot = Math.Min(BasePage.FreeIndexSlot(length), (byte)3);

            // check for avaiable re-usable page
            for(int currentSlot = startSlot; currentSlot >= 0; currentSlot--)
            {
                var freePageID = freeLists[currentSlot];

                // there is no free page here, try find princess in another castle
                if (freePageID == uint.MaxValue) continue;

                var page = this.GetPage<T>(freePageID);

                var newSlot = BasePage.FreeIndexSlot((byte)(page.FreeBlocks - length));

                // if slots will change, fix now
                if (currentSlot != newSlot)
                {
                    this.RemoveFreeList<T>(page, ref freeLists[currentSlot]);
                    this.AddFreeList<T>(page, ref freeLists[newSlot]);
                }

                // mark page page as dirty
                page.IsDirty = true;

                return page;
            }

            // if not page avaiable, create new and add in free list
            var newPage = this.NewPage<T>();

            // get slot based on how many blocks page will have after use
            var slot = BasePage.FreeIndexSlot((byte)(newPage.FreeBlocks - length));

            // and add into free-list
            this.AddFreeList<T>(newPage, ref freeLists[slot]);

            return newPage;
        }

        /// <summary>
        /// Add/Remove page into free slots on collection page. Used when some data are removed/changed
        /// For insert data, this method are called from GetFreePage
        /// </summary>
        public void AddOrRemoveFreeList<T>(T page, int initialSlot) where T : BasePage
        {
            ENSURE(page.PageType == PageType.Index || page.PageType == PageType.Data, "only index/data page contains free linked-list");

            var newSlot = BasePage.FreeIndexSlot(page.FreeBlocks);

            // there is no slot change - just exit (no need any change)
            if (newSlot == initialSlot) return;

            // select if I will get from free index list or data list
            var freeLists = page.PageType == PageType.Index ?
                _collectionPage.FreeIndexPageID :
                _collectionPage.FreeDataPageID;

            // remove from intial slot
            this.RemoveFreeList<T>(page, ref freeLists[initialSlot]);

            // if there is no items, delete page
            if (page.ItemsCount == 0)
            {
                this.DeletePage(page);
            }
            else
            {
                // add into current slot
                this.AddFreeList<T>(page, ref freeLists[newSlot]);
            }
        }

        /// <summary>
        /// Add page into double linked-list (always add as first element)
        /// </summary>
        private void AddFreeList<T>(T page, ref uint startPageID) where T : BasePage
        {
            ENSURE(page.PrevPageID == uint.MaxValue && page.NextPageID == uint.MaxValue, "only non-linked page can be added in linked list");

            // fix first/next page
            if (startPageID != uint.MaxValue)
            {
                var next = this.GetPage<T>(startPageID);
                next.PrevPageID = page.PageID;
                next.IsDirty = true;
            }

            page.PrevPageID = uint.MaxValue;
            page.NextPageID = startPageID;

            startPageID = page.PageID;

            _collectionPage.IsDirty = true;
        }

        /// <summary>
        /// Remove a page from double linked list.
        /// </summary>
        private void RemoveFreeList<T>(T page, ref uint startPageID) where T : BasePage
        {
            // fix prev page
            if (page.PrevPageID != uint.MaxValue)
            {
                var prev = this.GetPage<T>(page.PrevPageID);
                prev.NextPageID = page.NextPageID;
                prev.IsDirty = true;
            }

            // fix next page
            if (page.NextPageID != uint.MaxValue)
            {
                var next = this.GetPage<T>(page.NextPageID);
                next.PrevPageID = page.PrevPageID;
                next.IsDirty = true;
            }

            // if page is first of the list set firstPage as next page
            if (startPageID == page.PageID)
            {
                startPageID = page.NextPageID;
                _collectionPage.IsDirty = true;
            }

            // clear page pointer (MaxValue = not used)
            page.PrevPageID = page.NextPageID = uint.MaxValue;
        }

        #endregion

        public override string ToString()
        {
            return $"{_collectionName} (pages: {_localPages.Count})";
        }
    }
}