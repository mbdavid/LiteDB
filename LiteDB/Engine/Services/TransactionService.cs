using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// </summary>
    internal class TransactionService : IDisposable
    {
        // instances from Engine
        private LockService _locker;
        private WalService _wal;
        private FileService _datafile;
        private FileService _walfile;
        private Logger _log;

        // new instances
        private PageService _pager;
        private IndexService _index;
        private CollectionService _collection;
        private DataService _data;

        // transaction controls
        private Guid _transactionID;
        private int _readVersion;
        private string _colname;
        private LockControl _releaseLock;
        private Dictionary<uint, BasePage> _dirtyPages = new Dictionary<uint, BasePage>();
        private Dictionary<uint, PagePosition> _dirtyPagesWal = new Dictionary<uint, PagePosition>();

        /// <summary>
        /// Create new Transaction - if collection == null is read transaction
        /// </summary>
        public TransactionService(string collection, LockService locker, WalService wal, FileService datafile, FileService walfile, Logger log)
        {
            _locker = locker;
            _wal = wal;
            _datafile = datafile;
            _walfile = walfile;
            _log = log;

            _pager = new PageService(this, _log);
            _data = new DataService(this, _pager, _log);
            _index = new IndexService(this, _pager, _log);
            _collection = new CollectionService(this, _pager, _index, _data, _log);

            _colname = collection;
            _transactionID = Guid.NewGuid();
            _readVersion = _wal.CurrentVersion;

            _releaseLock = collection == null ? locker.Read() : locker.Write(collection);
        }

        /// <summary>
        /// Get a page from transaction, wal-index or datafile
        /// </summary>
        public T GetPage<T>(uint pageID)
            where T : BasePage
        {
            // first, look for this page inside local dirty pages
            if (_dirtyPages.TryGetValue(pageID, out var page))
            {
                return (T)page;
            }

            // if not inside dirtyPages, can be a dirty page already saved in wal file
            if (_dirtyPagesWal.TryGetValue(pageID, out var position))
            {
                return (T)_walfile.ReadPage(position.Position);
            }

            // now, look inside wal-index
            var pos = _wal.GetPageIndex(pageID, _readVersion);

            if (!pos.IsEmpty)
            {
                return (T)_walfile.ReadPage(pos.Position);
            }

            // for last way, look inside data file
            var pagePosition = BasePage.GetSizeOfPages(pageID);

            page = _datafile.ReadPage(pagePosition);

            return (T)page;
        }

        /// <summary>
        /// Must be call before any page change
        /// </summary>
        public T SetDirty<T>(BasePage page)
            where T : BasePage
        {
            // if transaction need change header page, lock header page to all other transactions (will be release with collection locker)
            if (page.PageID == 0)
            {
                _locker.Header(_colname);
            }

            // if page already in dirtyPages, just return
            if (_dirtyPages.TryGetValue(page.PageID, out var p))
            {
                return (T)p;
            }
            else
            {
                // clone before edit
                var clone = BasePage.ReadPage(page.WritePage());

                _dirtyPages[clone.PageID] = clone;

                return (T)clone;
            }
        }

        /// <summary>
        /// Write all dirty pages into WAL file and get page references. 
        /// Support write pages during transaction (before transaction finish). Useful for long transactions, like EnsureIndex in big collection
        /// </summary>
        public void WritePages()
        {
            // update page with transactionId in all dirty page
            var pages = _dirtyPages.Values.ForEach((i, p) => p.TransactionID = _transactionID);

            // save all dirty pages into disk and get pages position reference
            var saved = _walfile.WritePages(pages);

            // clear dirtyPage list
            _dirtyPages.Clear();

            // update dictionary with page position references
            foreach (var position in saved)
            {
                _dirtyPagesWal[position.PageID] = position;
            }
        }

        /// <summary>
        /// Write pages into disk and confirm transaction in wal-index
        /// </summary>
        public void Commit()
        {
            this.WritePages();

            _wal.Commit(_transactionID, _dirtyPagesWal.Values);
        }

        public void Dispose()
        {
            // clear any used dirty page
            _dirtyPages.Clear();
            _dirtyPagesWal.Clear();

            // release lock
            _releaseLock.Dispose();
        }
    }
}