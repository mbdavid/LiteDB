using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiteDB
{
    internal class WalService
    {
        private FileService _walfile = null;
        private FileService _datafile = null;
        private LockService _locker = null;

        private HashSet<Guid> _confirmedTransactions = new HashSet<Guid>();
        private ConcurrentDictionary<uint, ConcurrentDictionary<int, PagePosition>> _index = new ConcurrentDictionary<uint, ConcurrentDictionary<int, PagePosition>>();

        private int _currentReadVersion = 0;

        public WalService(LockService locker, FileService datafile, FileService walfile)
        {
            _locker = locker;
            _datafile = datafile;
            _walfile = walfile;
        }

        /// <summary>
        /// Represent the single instance of SharedPage (page 1)
        /// </summary>
        public SharedPage SharedPage { get; set; } = null;

        /// <summary>
        /// Get current read version for all new transactions
        /// </summary>
        public int CurrentReadVersion => _currentReadVersion;

        /// <summary>
        /// Checks if an Page/Version are in WAL-index memory. Consider version that are below parameter. Returns PagePosition of this page inside WAL-file or Empty if page doesn't found.
        /// </summary>
        public PagePosition GetPageIndex(uint pageID, int version)
        {
            // wal-index versions must be greater than 0 (version 0 is datafile)
            if (version == 0) return PagePosition.Empty;

            // get page slot in cache
            if (_index.TryGetValue(pageID, out var slot))
            {
                // get all page versions in wal-index
                // and then filter only equals-or-less then selected version
                var v = slot.Keys
                    .Where(x => x <= version)
                    .OrderByDescending(x => x)
                    .FirstOrDefault();

                // if versions on index are higher then request, exit
                if (v == 0) return PagePosition.Empty;

                // try get for concurrent dict this page (it's possible this page are no anymore in cache - other concurrency thread clear cache)
                if (slot.TryGetValue(v, out var position))
                {
                    return position;
                }
            }

            return PagePosition.Empty;
        }

        /// <summary>
        /// Confirm transaction ID version and return new current version number
        /// </summary>
        public int Commit(Guid transactionID, IEnumerable<PagePosition> pagePositions)
        {
            // add transaction to confirmed list
            _confirmedTransactions.Add(transactionID);

            // write confirmed shared page (use First() to execute)
            var confirm = _walfile.WritePagesSequence(new BasePage[] { new SharedPage(transactionID) }).First();

            // must lock commit operation to update index
            lock (_locker)
            {
                // increment current version
                _currentReadVersion++;

                // update wal-index
                foreach (var pos in pagePositions)
                {
                    this.AddPageToIndex(pos, _currentReadVersion);
                }

                return _currentReadVersion;
            }
        }

        /// <summary>
        /// Add new page position into wal-index
        /// </summary>
        private void AddPageToIndex(PagePosition pos, int version)
        {
            // get page slot in cache (or create if not exists)
            var slot = _index.GetOrAdd(pos.PageID, new ConcurrentDictionary<int, PagePosition>());

            // add page version to wal-index (update if already exists)
            slot.AddOrUpdate(version, pos, (v, old) => pos);
        }

        /// <summary>
        /// Do WAL checkpoint coping confirmed pages transaction from WAL file to datafile
        /// </summary>
        public void Checkpoint()
        {
            // if walfile are empty, just exit
            if (_walfile.IsEmpty()) return;

            // must enter in exclusive lock mode
            using (_locker.Exclusive())
            {
                // if there is not confirmed transaction
                if (_confirmedTransactions.Count == 0)
                {
                    // read all file get only confirmed transaction (with shared page in WAL)
                    foreach(var transactionID in _walfile
                            .ReadAllPages()
                            .Where(x => x.PageType == PageType.Shared)
                            .Select(x => x.TransactionID))
                    {
                        _confirmedTransactions.Add(transactionID);
                    }
                }

                // read all pages from WAL that are confirmed (exclude TransactionPages)
                // pages are in insert-order, do can re-write same pages many times with no problem (only last version will be valid)
                // clear TransactionID before write on datafile
                var walpages = _walfile
                    .ReadAllPages()
                    .Where(x => _confirmedTransactions.Contains(x.TransactionID))
                    .ForEach((i, p) => p.TransactionID = Guid.Empty);

                // write on datafile (pageID position based) for each wal page
                _datafile.WritePages(walpages).Execute();

                // clear wal-file and wal-index and current version
                _currentReadVersion = 0;
                _walfile.Clear();
                _index.Clear();
            }
        }
    }
}