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
        private WalService _wal;
        private FileService _datafile;
        private FileService _walfile;
        private Logger _log;

        // new instances
        private LockService _locker;
        private PageService _pager;
        private IndexService _index;
        private CollectionService _collection;
        private DataService _data;

        // transaction controls
        private Guid _transactionID;
        private int _readVersion;
        private LockReadWrite _lockReadWrite;
        private Dictionary<uint, BasePage> _local = new Dictionary<uint, BasePage>();
        private Dictionary<uint, PagePosition> _dirtyPagesWal = new Dictionary<uint, PagePosition>();

        // expose services
        public PageService Pager => _pager;
        public CollectionService Collection => _collection;
        public IndexService Indexer => _index;
        public DataService Data => _data;

        /// <summary>
        /// Create new Transaction - if collection == null is read transaction
        /// </summary>
        public TransactionService(LockService locker, WalService wal, FileService datafile, FileService walfile, Logger log)
        {
            _locker = locker;
            _wal = wal;
            _datafile = datafile;
            _walfile = walfile;
            _log = log;

            // need lock before get current read version
            _lockReadWrite = locker.Read();

            _pager = new PageService(this, _log);
            _data = new DataService(this, _pager, _log);
            _index = new IndexService(this, _pager, _log);
            _collection = new CollectionService(this, _pager, _index, _data, _log);

            _transactionID = Guid.NewGuid();
            _readVersion = _wal.CurrentVersion;
        }

        /// <summary>
        /// Write lock in defined collection - no more can write in this collection (readers only)
        /// </summary>
        public void WriteLock(string collection)
        {
            _locker.Write(_lockReadWrite, collection);
        }

        /// <summary>
        /// Lock header and reset transaction
        /// </summary>
        public void HeaderLock()
        {
            // checks if there is no dirty page
            if (_dirtyPagesWal.Count > 0 || _local.Any(x => x.Value.IsDirty)) throw new InvalidOperationException("Lock header are not supported when transaction contains dirty pages");

            _locker.Header(_lockReadWrite);

            // clear any local page (include clean ones)
            _local.Clear();

            // reset read version with most recent version
            _readVersion = _wal.CurrentVersion;
        }

        /// <summary>
        /// Get a page for this transaction: try local, wal-index or datafile. Must keep cloned instance of this page in this transaction
        /// </summary>
        public T GetPage<T>(uint pageID)
            where T : BasePage
        {
            // first, look for this page inside local pages
            if (_local.TryGetValue(pageID, out var page))
            {
                return (T)page;
            }

            // if not inside local pages, can be a dirty page already saved in wal file
            if (_dirtyPagesWal.TryGetValue(pageID, out var position))
            {
                var dirty = (T)_walfile.ReadPage(position.Position);

                // add into local pages
                _local[pageID] = dirty;

                return dirty;
            }

            // now, look inside wal-index
            var pos = _wal.GetPageIndex(pageID, _readVersion);

            if (!pos.IsEmpty)
            {
                var walpage = (T)_walfile.ReadPage(pos.Position);

                // copy to my local pages
                _local[pageID] = walpage;

                return walpage;
            }

            // for last chance, look inside original data file
            var pagePosition = BasePage.GetPagePostion(pageID);

            var filepage = (T)_datafile.ReadPage(pagePosition);

            _local[pageID] = filepage;

            return filepage;
        }

        /// <summary>
        /// Set page was dirty
        /// </summary>
        public void SetDirty(BasePage page, bool alwaysOverride = false)
        {
            // if is header page, lock header for other write transactions
            if (page.IsDirty == false && page.PageType == PageType.Header)
            {
                _locker.Header(_lockReadWrite);
            }

            if (page.IsDirty == false || alwaysOverride)
            {
                page.IsDirty = true;
                _local[page.PageID] = page;
            }
        }

        /// <summary>
        /// Write all dirty pages into WAL file and get page references. 
        /// Support write pages during transaction (before transaction finish). Useful for long transactions, like EnsureIndex in big collection
        /// </summary>
        public void WritePages()
        {
            // update pages with transactionId in all dirty page
            var dirty = _local.Values
                .Where(x => x.IsDirty)
                .ForEach((i, p) => p.TransactionID = _transactionID)
                .ToList();

            // save all dirty pages into walfile (sequencial mode) and get pages position reference
            var saved = _walfile.WritePagesSequencial(dirty)
                .ToList();

            // update dictionary with page position references
            foreach (var position in saved)
            {
                _dirtyPagesWal[position.PageID] = position;
            }

            // clear dirtyPage list
            _local.Clear();
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
            // clear any local/wal pages for this transaction
            _local.Clear();
            _dirtyPagesWal.Clear();

            // release lock
            _lockReadWrite.Dispose();
        }
    }
}