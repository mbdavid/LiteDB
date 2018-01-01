using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiteDB
{
    internal class PageService
    {
        private WalService _wal;
        private FileService _dataFile;
        private FileService _walFile;
        private HeaderPage _header;
        private Logger _log;

        private Guid _transactionID;
        private int _readVersion;
        private Dictionary<uint, BasePage> _localPages = new Dictionary<uint, BasePage>();
        private Dictionary<uint, PagePosition> _dirtyPagesWal = new Dictionary<uint, PagePosition>();

        // handle created pages during transaction (for rollback)
        private List<uint> _newPages = new List<uint>();


        /// <summary>
        /// Transaction ReadVersion
        /// </summary>
        public int ReadVersion => _readVersion;

        /// <summary>
        /// Transaction ID
        /// </summary>
        public Guid TransactionID => _transactionID;

        public PageService(HeaderPage header, WalService wal, FileService dataFile, FileService walFile, Logger log)
        {
            _header = header;
            _wal = wal;
            _dataFile = dataFile;
            _walFile = walFile;
            _log = log;
        }

        /// <summary>
        /// Inicializer pager instance, cleaning any loadad page and getting new ReadVersion
        /// </summary>
        public void Initialize()
        {
#if DEBUG
            if (_localPages.Any(x => x.Value.IsDirty) || _dirtyPagesWal.Count > 0) throw new InvalidOperationException("No dirty pages in transaction when initialize pager");
#endif
            _localPages.Clear();
            _transactionID = Guid.NewGuid();
            _readVersion = _wal.CurrentReadVersion;
        }

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
            if (_dirtyPagesWal.TryGetValue(pageID, out var position))
            {
                var dirty = (T)_walFile.ReadPage(position.Position);

                // add into local pages
                _localPages[pageID] = dirty;

                return dirty;
            }

            // now, look inside wal-index
            var pos = _wal.GetPageIndex(pageID, _readVersion);

            if (!pos.IsEmpty)
            {
                var walpage = (T)_walFile.ReadPage(pos.Position);

                // copy to my local pages
                _localPages[pageID] = walpage;

                return walpage;
            }

            // load header page (is a global single instance)
            if (pageID == 0)
            {
                return _header as T;
            }

            // for last chance, look inside original data file
            var pagePosition = BasePage.GetPagePostion(pageID);

            var filepage = (T)_dataFile.ReadPage(pagePosition);

            _localPages[pageID] = filepage;

            return filepage;
        }

        /// <summary>
        /// Set page was dirty
        /// </summary>
        public void SetDirty(BasePage page, bool alwaysOverride = false)
        {
            if (page.IsDirty == false || alwaysOverride)
            {
                page.IsDirty = true;
                _localPages[page.PageID] = page;
            }
        }

        /// <summary>
        /// Write all dirty pages into WAL file and get page references. 
        /// Support write pages during transaction (before transaction finish). Useful for long transactions, like EnsureIndex in big collection
        /// Clear all pages (including clean ones)
        /// </summary>
        public void PersistTransaction()
        {
            // update pages with transactionId in all dirty page
            var dirty = _localPages.Values
                .Where(x => x.IsDirty)
                .ForEach((i, p) => p.TransactionID = _transactionID)
                .ToList();

            // save all dirty pages into walfile (sequencial mode) and get pages position reference
            var saved = _walFile.WritePagesSequence(dirty)
                .ToList();

            // update dictionary with page position references
            foreach (var position in saved)
            {
                _dirtyPagesWal[position.PageID] = position;
            }

            // clear dirtyPage list
            _localPages.Clear();
        }

        /// <summary>
        /// Call wal commit using current transaction
        /// </summary>
        public void WalCommit()
        {
            // create new header page with transaction ID commit
            var copy = new HeaderPage()
            {
                TransactionID = _transactionID,
                FreeEmptyPageID = _header.FreeEmptyPageID,
                LastPageID = _header.LastPageID
            };

            _wal.Commit(copy, _dirtyPagesWal.Values);
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
        /// </summary>
        public T NewPage<T>(BasePage prevPage = null)
            where T : BasePage
        {
            var pageID = (uint)0;
            var diskData = new byte[0];

            // try get page from Empty free list
            if (_header.FreeEmptyPageID != uint.MaxValue)
            {
                //var free = _trans.GetPage<BasePage>(header.FreeEmptyPageID);
                //
                //// remove page from empty list
                //this.AddOrRemoveToFreeList(false, free, header, ref header.FreeEmptyPageID);
                //
                //pageID = free.PageID;
                throw new NotSupportedException();
            }
            else
            {
                // increase LastPageID from shared page
                lock (_header)
                {
                    pageID = ++_header.LastPageID;
                }
            }

            var page = BasePage.CreateInstance<T>(pageID);

            // add page to cache with correct T type (could be an old Empty page type)
            this.SetDirty(page, true);

            // if there a page before, just fix NextPageID pointer
            if (prevPage != null)
            {
                page.PrevPageID = prevPage.PageID;
                prevPage.NextPageID = page.PageID;

                this.SetDirty(prevPage);
            }

            // retain a list of created pages to, in a rollback situation, back pages to empty list
            _newPages.Add(page.PageID);

            return page;
        }

        /// <summary>
        /// Return added pages when occurs an rollback transaction (run this only in rollback). Create new transactionID and add into
        /// WAL file all new pages as EmptyPage in a linked order - also, update SharedPage before store
        /// </summary>
        public void ReturnNewPages()
        {
            // if no new pages, just exit
            if (_newPages.Count == 0) return;

            var pages = new List<EmptyPage>();

            // create new transaction ID
            var transactionID = Guid.NewGuid();

            // create list of empty pages with forward link pointer
            for (var i = 0; i < _newPages.Count; i++)
            {
                var pageID = _newPages[i];
                var next = i < _newPages.Count - 1 ? _newPages[i + 1] : uint.MaxValue;

                pages.Add(new EmptyPage(pageID)
                {
                    NextPageID = next,
                    TransactionID = transactionID,
                    IsDirty = true
                });
            }

            // persist all pages into wal-file
            var pagePositions = _walFile.WritePagesSequence(pages)
                .ToList();

            // now lock header to update FreePageList
            lock (_header)
            {
                // create copy of header page to sendo to wal file
                var copy = new HeaderPage
                {
                    TransactionID = transactionID,
                    FreeEmptyPageID = pages.First().PageID, // in this header page free empty list starts with my new empty list page
                    LastPageID = _header.LastPageID
                };

                // and my last page will link to current first link
                pages.Last().NextPageID = _header.FreeEmptyPageID;

                // now commit last confirm page to wal file
                _wal.Commit(copy, pagePositions);

                // now can update global header version
                _header.FreeEmptyPageID = copy.FreeEmptyPageID;
            }

        }

        /// <summary>
        /// Delete an page using pageID - transform them in Empty Page and add to EmptyPageList
        /// If you delete a page, you can using same old instance of page - page will be converter to EmptyPage
        /// If need deleted page, use GetPage again
        /// </summary>
        public void DeletePage(uint pageID, bool addSequence = false)
        {
            throw new NotSupportedException();

//            // get all pages in sequence or a single one
//            var pages = addSequence ? this.GetSeqPages<BasePage>(pageID).ToArray() : new BasePage[] { _trans.GetPage<BasePage>(pageID) };
//
//            // get my header page
//            var header = _trans.GetPage<HeaderPage>(0);
//
//            // adding all pages to FreeList
//            foreach (var page in pages)
//            {
//                // create a new empty page based on a normal page
//                var empty = new EmptyPage(page.PageID);
//
//                // add empty page to dirty pages in transaction
//                _trans.SetDirty(empty, true);
//
//                // add to empty free list
//                this.AddOrRemoveToFreeList(true, empty, header, ref header.FreeEmptyPageID);
//            }
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
    }
}