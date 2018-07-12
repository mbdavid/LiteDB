using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal class WalService
    {
        private LockService _locker;
        private DataFileService _dataFile;
        private WalFileService _walFile;
        private Logger _log;

        private HashSet<Guid> _confirmedTransactions = new HashSet<Guid>();
        private ConcurrentDictionary<uint, ConcurrentDictionary<int, long>> _index = new ConcurrentDictionary<uint, ConcurrentDictionary<int, long>>();

        private int _currentReadVersion = 0;

        public WalFileService WalFile => _walFile;
        public ConcurrentDictionary<uint, ConcurrentDictionary<int, long>> Index => _index;
        public HashSet<Guid> ConfirmedTransactions => _confirmedTransactions;

        public WalService(LockService locker, DataFileService dataFile, IDiskFactory factory, TimeSpan timeout, long sizeLimit, bool utcDate, Logger log)
        {
            _locker = locker;
            _dataFile = dataFile;
            _log = log;

            _walFile = new WalFileService(factory, timeout, sizeLimit, utcDate, log);

            this.LoadConfirmedTransactions();
        }

        /// <summary>
        /// Get current read version for all new transactions
        /// </summary>
        public int CurrentReadVersion => _currentReadVersion;

        /// <summary>
        /// Checks if a Page/Version are in WAL-index memory. Consider version that are below parameter. Returns PagePosition of this page inside WAL-file or Empty if page doesn't found.
        /// </summary>
        public long GetPageIndex(uint pageID, int version)
        {
            // wal-index versions must be greater than 0 (version 0 is datafile)
            if (version == 0) return long.MaxValue;

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
                if (v == 0) return long.MaxValue;

                // try get for concurrent dict this page (it's possible this page are no anymore in cache - other concurrency thread clear cache)
                if (slot.TryGetValue(v, out var position))
                {
                    return position;
                }
            }

            return long.MaxValue;
        }

        /// <summary>
        /// Write last confirmation page into all and update all indexes
        /// </summary>
        public void ConfirmTransaction(HeaderPage confirm, IEnumerable<PagePosition> pagePositions)
        {
            // mark confirm page as dirty
            confirm.IsDirty = true;

            // write header-confirm transaction page in wal file
            _walFile.WritePages(new HeaderPage[] { confirm }, null);

            // add confirm page into confirmed-queue to be used in checkpoint
            _confirmedTransactions.Add(confirm.TransactionID);

            // must lock commit operation to update WAL-Index (memory only operation)
            lock (_index)
            {
                // increment current version
                _currentReadVersion++;

                // update wal-index
                foreach (var pos in pagePositions)
                {
                    // get page slot in _index (by pageID) (or create if not exists)
                    var slot = _index.GetOrAdd(pos.PageID, new ConcurrentDictionary<int, long>());

                    // add page version (update if already exists)
                    slot.AddOrUpdate(_currentReadVersion, pos.Position, (v, old) => pos.Position);
                }
            }
        }

        /// <summary>
        /// Do WAL checkpoint coping confirmed pages transaction from WAL file to datafile. Return how many pages was copied from WAL file to data file
        /// This process are SYNC and disk was FLUSHED
        /// If header was passed, update with last checkpoint date/counter
        /// </summary>
        public int Checkpoint(bool deleteWalFile, HeaderPage header, bool lockReserved)
        {
            // checkpoint can run only without any open transaction in current thread
            if (_locker.IsInTransaction) throw LiteException.InvalidTransactionState("Checkpoint", TransactionState.Active);

            if (lockReserved)
            {
                // enter in special database reserved lock (wait all readers/writers)
                // after this, everyone can read but no others can write
                _locker.EnterReserved(false);
            }

            try
            {
                if (_walFile.HasPages() == false)
                {
                    if (deleteWalFile && _walFile.Delete() == false)
                    {
                        DEBUG(true, "WAL file was not deleted because are not empty");
                    }

                    return 0;
                }

                _log.Info("checkpoint" + (deleteWalFile ? " with delete WAL file" : ""));

                var count = 0;

                HeaderPage last = null;

                // get all pages inside WAL file and contains valid confirmed pages
                var pages = _walFile.ReadPages()
                    .Where(x => _confirmedTransactions.Contains(x.TransactionID))
#if DEBUG
                    .ForEach((i, x) => DEBUG(x.TransactionID == Guid.Empty, "pages in wal must have transaction id"))
#endif
                    .ForEach((i, x) =>
                    {
                        x.TransactionID = Guid.Empty;

                        if (x.PageType == PageType.Header)
                        {
                            last = x as HeaderPage;
                            last.LastCheckpoint = DateTime.Now;
                        }
                    });

                // write page on data disk
                _dataFile.WritePages(pages);
                
                // update single header instance with last confirmed header
                if (last != null)
                {
                    // update header checkpoint datetime
                    if (header != null)
                    {
                        header.LastCheckpoint = last.LastCheckpoint;
                    }

                    // shrink datafile if length are large than needed
                    var length = BasePage.GetPagePosition(last.LastPageID + 1);

                    if (length < _dataFile.Length)
                    {
                        _dataFile.SetLength(length);
                    }
                }

                // now, all wal pages are saved in data disk - can clear walfile
                _walFile.Clear();

                // delete wal file
                if (deleteWalFile && _walFile.Delete() == false)
                {
                    DEBUG(true, "WAL file was not deleted because are not empty");
                }

                // clear indexes/confirmed transactions
                _index.Clear();
                _confirmedTransactions.Clear();
                _currentReadVersion = 0;

                return count;
            }
            finally
            {
                if (lockReserved)
                {
                    _locker.ExitReserved(false);
                }
            }
        }

        /// <summary>
        /// Load all confirmed transactions from WAL file (used only when open datafile)
        /// </summary>
        private void LoadConfirmedTransactions()
        {
            if (_walFile.HasPages() == false) return;

            // read all pages to get confirmed transactions
            var items = _walFile.ReadPages()
                .Where(x => x.PageType == PageType.Header)
                .Select(x => x.TransactionID);

            _confirmedTransactions.AddRange(items);
        }
    }
}