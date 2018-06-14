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
        private readonly DataFileService _dataFile;
        private readonly WalService _wal;

        // instances from transaction
        private readonly TransactionPages _transPages;

        // snapshot controls
        private readonly string _collectionName;
        private SnapshotMode _mode;
        private CollectionPage _collectionPage;
        private int _readVersion;

        private Dictionary<uint, BasePage> _localPages = new Dictionary<uint, BasePage>();

        // expose services
        public int ReadVersion => _readVersion;
        public Dictionary<uint, BasePage> LocalPages => _localPages;
        public SnapshotMode Mode => _mode;

        public Snapshot(SnapshotMode mode, string collectionName, HeaderPage header, TransactionPages transPages, LockService locker, DataFileService dataFile, WalService wal)
        {
            _header = header;
            _transPages = transPages;
            _locker = locker;
            _dataFile = dataFile;
            _wal = wal;

            _collectionName = collectionName;
            _mode = mode;

            // enter in lock mode according initial mode
            if (_mode == SnapshotMode.Read)
            {
                _locker.EnterRead(_collectionName);
            }
            else
            {
                _locker.EnterReserved(_collectionName);
            }

            // initialize version and get read version
            this.Initialize();
        }

        /// <summary>
        /// Enter snapshot in write mode (if not already in write mode)
        /// </summary>
        public void WriteMode(bool addIfNotExits)
        {
            if (_mode == SnapshotMode.Read)
            {
                // enter in reserved mode (exit read first)
                _locker.ExitRead(_collectionName);
                _locker.EnterReserved(_collectionName);

                _mode = SnapshotMode.Write;

                // need initialize cache and wal version
                this.Initialize();
            }

            if (addIfNotExits && this.CollectionPage == null)
            {
                var srv = new CollectionService(this, _header, _transPages);

                // create new collection and add into transaction
                _collectionPage = srv.Add(_collectionName);
            }
        }

        /// <summary>
        /// Get/Set collection reference. Returns null if not exists in collections
        /// </summary>
        public CollectionPage CollectionPage
        {
            get
            {
                if (_collectionPage == null)
                {
                    var srv = new CollectionService(this, _header, _transPages);

                    _collectionPage = srv.Get(_collectionName);
                }

                return _collectionPage;
            }
            set
            {
                _collectionPage = value;
            }
        }

        /// <summary>
        /// Create instance of collection service using snapshot variables
        /// </summary>
        public CollectionService GetCollectionService()
        {
            return new CollectionService(this, _header, _transPages);
        }

        /// <summary>
        /// Unlock all collections locks on dispose
        /// </summary>
        public void Dispose()
        {
            if (_mode == SnapshotMode.Read)
            {
                _locker.ExitRead(_collectionName);
            }
            else
            {
                _locker.ExitReserved(_collectionName);
            }
        }

        /// <summary>
        /// Initializer snapshot instance, cleaning any loadad page and getting new ReadVersion
        /// </summary>
        private void Initialize()
        {
            DEBUG(_localPages.Where(x => x.Value.IsDirty).Count() > 0, "Snapshot initialize cann't contains dirty pages");

            _localPages.Clear();
            _readVersion = _wal.CurrentReadVersion;
            _collectionPage = null;
        }

        #region Page Version functions

        /// <summary>
        /// Get a page for this transaction: try local, wal-index or datafile. Must keep cloned instance of this page in this transaction
        /// </summary>
        public T GetPage<T>(uint pageID)
            where T : BasePage
        {
            // first, look for this page inside local pages
            if (_localPages.TryGetValue(pageID, out var page))
            {
                return (T)page;
            }

            // if not inside local pages, can be a dirty page already saved in wal file
            if (_transPages.DirtyPagesWal.TryGetValue(pageID, out var position))
            {
                var dirty = (T)_wal.WalFile.ReadPage(position.Position, _mode == SnapshotMode.Write);

                // add into local pages
                _localPages[pageID] = dirty;

                // increment transaction size counter
                _transPages.TransactionSize++;

                return dirty;
            }

            // now, look inside wal-index
            var pos = _wal.GetPageIndex(pageID, _readVersion);

            if (pos != long.MaxValue)
            {
                var walpage = (T)_wal.WalFile.ReadPage(pos, _mode == SnapshotMode.Write);

                // copy to my local pages
                _localPages[pageID] = walpage;

                // increment transaction page counter
                _transPages.TransactionSize++;

                return walpage;
            }

            // load header page (is a global single instance)
            if (pageID == 0)
            {
                return _header as T;
            }

            // for last chance, look inside original disk data file
            var pagePosition = BasePage.GetPagePosition(pageID);

            var diskpage = (T)_dataFile.ReadPage(pagePosition, _mode == SnapshotMode.Write);

            // add this page into local pages
            _localPages[pageID] = diskpage;

            // increment transaction size counter
            _transPages.TransactionSize++;

            return diskpage;
        }

        /// <summary>
        /// Set page was dirty
        /// </summary>
        public void SetDirty(BasePage page)
        {
            // mark as dirty only clean pages
            if (page.IsDirty == false)
            {
                page.IsDirty = true;
                _localPages[page.PageID] = page;

                // increment transaction size counter
                _transPages.TransactionSize++;
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
        public T NewPage<T>(BasePage prevPage = null)
            where T : BasePage
        {
            var pageID = 0u;

            // lock header instance to get new page
            lock (_header)
            {
                // try get page from Empty free list
                if (_header.FreeEmptyPageID != uint.MaxValue)
                {
                    var free = this.GetPage<BasePage>(_header.FreeEmptyPageID);

                    // set header free empty page to next free page
                    _header.FreeEmptyPageID = free.NextPageID;

                    pageID = free.PageID;
                }
                else
                {
                    // increase LastPageID from shared page
                    pageID = ++_header.LastPageID;
                }
            }

            var page = BasePage.CreateInstance<T>(pageID);

            // add page to cache with correct T type (could be an old Empty page type)
            // page.IsDirty = false so will override on index
            this.SetDirty(page);

            // if there a page before, just fix NextPageID pointer
            if (prevPage != null)
            {
                page.PrevPageID = prevPage.PageID;
                prevPage.NextPageID = page.PageID;

                this.SetDirty(prevPage);
            }

            // retain a list of created pages to, in a rollback situation, back pages to empty list
            _transPages.NewPages.Add(page.PageID);

            return page;
        }

        /// <summary>
        /// Delete an page using pageID - transform them in Empty Page and add to EmptyPageList
        /// If you delete a page, you can using same old instance of page - page will be converter to EmptyPage
        /// If need deleted page, use GetPage again
        /// </summary>
        public void DeletePage(uint pageID, bool addSequence = false)
        {
            // get all pages in sequence or a single one
            var pages = addSequence ? this.GetSeqPages<BasePage>(pageID) : new BasePage[] { this.GetPage<BasePage>(pageID) };

            // adding all pages into a sequence using only NextPageID
            foreach (var page in pages)
            {
                // create a new empty page based on a normal page
                var empty = new EmptyPage(page.PageID);

                // add empty page to dirty pages in transaction
                this.SetDirty(empty);

                if (_transPages.FirstDeletedPage == null)
                {
                    // if first page in sequence
                    _transPages.FirstDeletedPage = empty;
                    _transPages.LastDeletedPage = empty;
                }
                else if(_transPages.FirstDeletedPage.PageID == _transPages.LastDeletedPage.PageID)
                {
                    // if is second page on sequence
                    _transPages.FirstDeletedPage.NextPageID = empty.PageID;
                    _transPages.LastDeletedPage = empty;
                }
                else
                {
                    // if any other page, link last with current page
                    _transPages.LastDeletedPage.NextPageID = empty.PageID;
                    _transPages.LastDeletedPage = empty;
                }

                _transPages.DeletedPages++;
            }
        }

        /// <summary>
        /// Returns a page that contains space enough to data to insert new object - if one does not exit, creates a new page.
        /// </summary>
        public T GetFreePage<T>(uint startPageID, int size)
            where T : BasePage
        {
            if (startPageID != uint.MaxValue)
            {
                // get the first page
                var page = this.GetPage<T>(startPageID);

                // check if there space in this page
                var free = page.FreeBytes;

                // first, test if there is space on this page
                if (free >= size)
                {
                    return page;
                }
            }

            // if not has space on first page, there is no page with space (pages are ordered), create a new one
            return this.NewPage<T>();
        }

        #endregion

        #region Add Or Remove do empty list

        /// <summary>
        /// Add or Remove a page in a sequence
        /// </summary>
        /// <param name="add">Indicate that will add or remove from FreeList</param>
        /// <param name="page">Page to add or remove from FreeList</param>
        /// <param name="startPage">Page reference where start the header list node</param>
        /// <param name="fieldPageID">Field reference, from startPage</param>
        public void AddOrRemoveToFreeList(bool add, BasePage page, BasePage startPage, ref uint fieldPageID)
        {
            if (add)
            {
                // if page has no prev/next it's not on list - lets add
                if (page.PrevPageID == uint.MaxValue && page.NextPageID == uint.MaxValue)
                {
                    this.AddToFreeList(page, startPage, ref fieldPageID);
                }
                else
                {
                    // otherwise this page is already in this list, lets move do put in free size desc order
                    this.MoveToFreeList(page, startPage, ref fieldPageID);
                }
            }
            else
            {
                // if this page is not in sequence, its not on freelist
                if (page.PrevPageID == uint.MaxValue && page.NextPageID == uint.MaxValue)
                    return;

                this.RemoveToFreeList(page, startPage, ref fieldPageID);
            }
        }

        /// <summary>
        /// Add a page in free list in desc free size order
        /// </summary>
        private void AddToFreeList(BasePage page, BasePage startPage, ref uint fieldPageID)
        {
            var free = page.FreeBytes;
            var nextPageID = fieldPageID;
            BasePage next = null;

            // let's page in desc order
            while (nextPageID != uint.MaxValue)
            {
                next = this.GetPage<BasePage>(nextPageID);

                if (free >= next.FreeBytes)
                {
                    // assume my page in place of next page
                    page.PrevPageID = next.PrevPageID;
                    page.NextPageID = next.PageID;

                    // link next page to my page
                    next.PrevPageID = page.PageID;

                    // mark next page as dirty
                    this.SetDirty(next);
                    this.SetDirty(page);

                    // my page is the new first page on list
                    if (page.PrevPageID == 0)
                    {
                        fieldPageID = page.PageID;
                        this.SetDirty(startPage); // fieldPageID is from startPage
                    }
                    else
                    {
                        // if not the first, ajust links from previous page (set as dirty)
                        var prev = this.GetPage<BasePage>(page.PrevPageID);
                        prev.NextPageID = page.PageID;
                        this.SetDirty(prev);
                    }

                    return; // job done - exit
                }

                nextPageID = next.NextPageID;
            }

            // empty list, be the first
            if (next == null)
            {
                // it's first page on list
                page.PrevPageID = 0;
                fieldPageID = page.PageID;

                this.SetDirty(startPage);
            }
            else
            {
                // it's last position on list (next = last page on list)
                page.PrevPageID = next.PageID;
                next.NextPageID = page.PageID;

                this.SetDirty(next);
            }

            // set current page as dirty
            this.SetDirty(page);
        }

        /// <summary>
        /// Remove a page from list - the ease part
        /// </summary>
        private void RemoveToFreeList(BasePage page, BasePage startPage, ref uint fieldPageID)
        {
            // this page is the first of list
            if (page.PrevPageID == 0)
            {
                fieldPageID = page.NextPageID;
                this.SetDirty(startPage); // fieldPageID is from startPage
            }
            else
            {
                // if not the first, get previous page to remove NextPageId
                var prevPage = this.GetPage<BasePage>(page.PrevPageID);
                prevPage.NextPageID = page.NextPageID;
                this.SetDirty(prevPage);
            }

            // if my page is not the last on sequence, adjust the last page (set as dirty)
            if (page.NextPageID != uint.MaxValue)
            {
                var nextPage = this.GetPage<BasePage>(page.NextPageID);
                nextPage.PrevPageID = page.PrevPageID;
                this.SetDirty(nextPage);
            }

            page.PrevPageID = page.NextPageID = uint.MaxValue;

            // mark page that will be removed as dirty
            this.SetDirty(page);
        }

        /// <summary>
        /// When a page is already on a list it's more efficient just move comparing with siblings
        /// </summary>
        private void MoveToFreeList(BasePage page, BasePage startPage, ref uint fieldPageID)
        {
            //TODO: write a better solution
            this.RemoveToFreeList(page, startPage, ref fieldPageID);
            this.AddToFreeList(page, startPage, ref fieldPageID);
        }

        #endregion

        public override string ToString()
        {
            return _collectionName + " (pages: " + this.LocalPages.Count + ")";
        }
    }
}