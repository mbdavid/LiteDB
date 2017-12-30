using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiteDB
{
    internal class WalService
    {
        private HashSet<Guid> _confirmedTransactions = new HashSet<Guid>();
        ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, PagePosition>> _pagesToConfirm;

        private int _currentVersion = 0;

        ConcurrentDictionary<uint, ConcurrentDictionary<int, PagePosition>> _index;

        private FileService _walfile = null;
        private FileService _datafile = null;
        private LockService _locker = null;

        private object _commitLocker = new object();

        public int CurrentVersion => _currentVersion;

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
                // get all page versions avaiable in wal-index page slot
                var versions = slot.Keys.OrderBy(x => x);

                // get best version for request verion
                var v = versions.FirstOrDefault(x => x <= version);

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

            // write confirmed transaction page
            _walfile.WritePagesSequencial(new BasePage[] { new TransactionPage(transactionID) });

            // must lock commit operation to update index
            lock (_commitLocker)
            {
                // increment current version
                _currentVersion++;

                // update wal-index
                foreach (var pos in pagePositions)
                {
                    this.AddPageToIndex(pos, _currentVersion);
                }

                return _currentVersion;
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
                    // read all file get only confirmed transaction
                    foreach(var transactionID in _walfile
                            .ReadAllPages()
                            .Where(x => x.PageType == PageType.Transaction)
                            .Select(x => x.TransactionID))
                    {
                        _confirmedTransactions.Add(transactionID);
                    }
                }

                // read all pages from WAL that are confirmed (exclude TransactionPages)
                // pages are in insert-order, do can re-write same pages many times with no problem (only last version will be valid)
                var walpages = _walfile
                    .ReadAllPages()
                    .Where(x => _confirmedTransactions.Contains(x.TransactionID) && x.PageType != PageType.Transaction);

                // write on datafile each wal page
                _datafile.WritePages(walpages);

                // clear wal-file and wal-index and current version
                _currentVersion = 0;
                _walfile.Clear();
                _index.Clear();
            }
        }
    }
}