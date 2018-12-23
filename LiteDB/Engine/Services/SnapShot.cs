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
        private readonly uint _transactionID;
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

        public Snapshot(LockMode mode, string collectionName, HeaderPage header, uint transactionID, TransactionPages transPages, LockService locker, WalIndexService walIndex, DiskReader reader, bool addIfNotExists)
        {
            _mode = mode;
            _collectionName = collectionName;
            _header = header;
            _transactionID = transactionID;
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

            // clear local pages (will clear _collectionPage link reference)
            if (_collectionPage != null)
            {
                _localPages.Remove(_collectionPage.PageID);
            }
        }

        /// <summary>
        /// Get writable pages (only if snapshot mode is Write). Can be Dirty (changed) or Clean 
        /// </summary>
        public IEnumerable<BasePage> GetWritablePages(bool dirty, bool includeCollectionPage)
        {
            // if snapshot is read only, just exit
            if (_mode == LockMode.Read) yield break;

            foreach(var page in _localPages.Values.Where(x => x.IsDirty == dirty))
            {
                ENSURE(page.PageType != PageType.Header && page.PageType != PageType.Collection, "local cache cann't contains this page type");

                yield return page;
            }

            if (includeCollectionPage && _collectionPage != null && _collectionPage.IsDirty == dirty)
            {
                yield return _collectionPage;
            }
        }

        /// <summary>
        /// Clear all local pages and return page buffer to file reader. Do not release CollectionPage (only in Dispose method)
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
            // release all data/index pages
            this.Clear();

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
            ENSURE(pageID <= _header.LastPageID, "request page must be less or equals lastest page in data file");

            // first, look for this page inside local pages
            if (_localPages.TryGetValue(pageID, out var page))
            {
                return (T)page;
            }

            // if page is not in local cache, get from disk (log/wal/data)
            page = this.ReadPage<T>(pageID);

            // add into local pages
            _localPages[pageID] = page;

            // increment transaction size counter
            _transPages.TransactionSize++;

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

                ENSURE(dirty.TransactionID == _transactionID, "this page must came from same transaction");

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
                // for last chance, look inside original disk data file
                var pagePosition = BasePage.GetPagePosition(pageID);

                // read page from data file
                var buffer = _reader.ReadPage(pagePosition, _mode == LockMode.Write, FileOrigin.Data);
                var diskpage = BasePage.ReadPage<T>(buffer);

                ENSURE(diskpage.IsConfirmed == false || diskpage.TransactionID != 0, "page are not header-clear in data file");

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
                // there is need for _header.Savepoint() because changes here will incremental and will be persist later
                // if any problem occurs here, rollback will catch this changes

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

                // retain a list of created pages to, in a rollback situation, back pages to empty list
                _transPages.NewPages.Add(pageID);
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
            // remove from linked-list
            ENSURE(page.PrevPageID == uint.MaxValue && page.NextPageID == uint.MaxValue, "before delete a page, no linked list with any another page");

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

            // update localPage to new Empty Page
            page = new BasePage(page.GetBuffer(false), page.PageID, PageType.Empty);

            page.IsDirty = true;

            _localPages[page.PageID] = page;

            _transPages.DeletedPages++;
        }

        /// <summary>
        /// Returns a page that contains space enough to data to insert new object - if one does not exit, creates a new page.
        /// Before return page, fix empty free list slot according with passed length
        /// </summary>
        public T GetFreePage<T>(int bytesLength)
            where T : BasePage
        {
            var length = bytesLength + PageSlot.SIZE; // add +4 bytes for footer slot

            // select if I will get from free index list or data list
            var freeLists = typeof(T) == typeof(IndexPage) ?
                _collectionPage.FreeIndexPageID :
                _collectionPage.FreeDataPageID;

            // get minimum slot to check for free page. Returns -1 if need NewPage
            var startSlot = BasePage.GetMinimumIndexSlot(length);

            // check for avaiable re-usable page
            for(int currentSlot = startSlot; currentSlot >= 0; currentSlot--)
            {
                var freePageID = freeLists[currentSlot];

                // there is no free page here, try find princess in another castle
                if (freePageID == uint.MaxValue) continue;

                var page = this.GetPage<T>(freePageID);

                var newSlot = BasePage.FreeIndexSlot(page.FreeBytes - length);

                // if slots will change, fix now
                if (currentSlot != newSlot)
                {
                    this.RemoveFreeList<T>(page, ref freeLists[currentSlot]);
                    this.AddFreeList<T>(page, ref freeLists[newSlot]);
                }

                ENSURE(page.FreeBytes >= length, "ensure selected page has space enougth for this content");

                // mark page page as dirty
                page.IsDirty = true;

                return page;
            }

            // if not page avaiable, create new and add in free list
            var newPage = this.NewPage<T>();

            // get slot based on how many blocks page will have after use
            var slot = BasePage.FreeIndexSlot(newPage.FreeBytes - length - PageSlot.SIZE);

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

            var newSlot = BasePage.FreeIndexSlot(page.FreeBytes);

            // there is no slot change - just exit (no need any change) [except if has no more items)
            if (newSlot == initialSlot && page.ItemsCount > 0) return;

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