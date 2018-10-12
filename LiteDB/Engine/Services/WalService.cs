using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal class WalService : IDisposable
    {
        private readonly LockService _locker;
        private readonly DataFileService _dataFile;
        private readonly WalFileService _walFile;
        private readonly Logger _log;

        private readonly List<ObjectId> _confirmedTransactions = new List<ObjectId>();
        private readonly ConcurrentDictionary<uint, ConcurrentDictionary<int, long>> _index = new ConcurrentDictionary<uint, ConcurrentDictionary<int, long>>();

        private int _currentReadVersion = 0;

        public WalFileService WalFile => _walFile;
        public ConcurrentDictionary<uint, ConcurrentDictionary<int, long>> Index => _index;
        public List<ObjectId> ConfirmedTransactions => _confirmedTransactions;

        public WalService(LockService locker, DataFileService dataFile, IDiskFactory factory, long sizeLimit, bool utcDate, Logger log)
        {
            _locker = locker;
            _dataFile = dataFile;
            _log = log;

            _walFile = new WalFileService(factory, sizeLimit, utcDate, log);
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
        /// Add transactionID in confirmed list and update WAL index with all pages positions
        /// </summary>
        public void ConfirmTransaction(ObjectId transactionID, ICollection<PagePosition> pagePositions)
        {
            // add confirm page into confirmed-queue to be used in checkpoint
            _confirmedTransactions.Add(transactionID);

            // if no pages was saved, just exit
            if (pagePositions.Count == 0) return;

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
        /// Do WAL checkpoint coping confirmed pages from WAL file to datafile.
        /// This first version works only doing full checkpoint with reserved lock
        /// Return how many pages was copied from WAL file to data file
        /// </summary>
        public int Checkpoint(HeaderPage header, bool lockReserved)
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
                // if wal already clean (or not exists)
                if (_walFile.Length == 0) return 0;

                _log.Info("checkpoint");

                var count = 0;

                HeaderPage last = null;

                var sortedConfirmTransactions = new HashSet<ObjectId>(_confirmedTransactions);

                // get all pages inside WAL file and contains valid confirmed pages
                var pages = _walFile.ReadPages(true)
                    .Where(x => sortedConfirmTransactions.Contains(x.TransactionID))
                    .ForEach((i, x) =>
                    {
                        count++;
                        x.TransactionID = ObjectId.Empty;
                        x.IsConfirmed = false;

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
                    // shrink datafile if length are large than needed
                    var length = BasePage.GetPagePosition(last.LastPageID + 1);

                    if (length < _dataFile.Length)
                    {
                        _dataFile.SetLength(length);
                    }
                }

                // update header checkpoint datetime
                if (header != null)
                {
                    header.LastCheckpoint = DateTime.Now;
                }

                // now, all wal pages are saved in data disk - can clear walfile
                _walFile.Clear();

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
        /// Don't need lock because it's called on ctor of LiteEngine
        /// </summary>
        public void RestoreIndex(ref HeaderPage header)
        {
            if (_walFile.Length == 0) return;

            // get all page positions
            var positions = new Dictionary<ObjectId, List<PagePosition>>();
            var current = 0L;

            // read all pages to get confirmed transactions (do not read page content, only page header)
            foreach(var page in _walFile.ReadPages(false))
            {
                var position = new PagePosition(page.PageID, current);

                if (positions.TryGetValue(page.TransactionID, out var list))
                {
                    list.Add(position);
                }
                else
                {
                    positions[page.TransactionID] = new List<PagePosition> { position };
                }

                if (page.IsConfirmed)
                {
                    this.ConfirmTransaction(page.TransactionID, positions[page.TransactionID]);

                    if (page.PageType == PageType.Header)
                    {
                        // if confirmed page is header need realod full page from WAL (current page contains only header data)
                        header = _walFile.ReadPage(current) as HeaderPage;
                        header.TransactionID = ObjectId.Empty;
                        header.IsConfirmed = false;
                    }
                }

                current += PAGE_SIZE;
            }
        }

        public void Dispose()
        {
            _walFile.Dispose();
        }
    }
}