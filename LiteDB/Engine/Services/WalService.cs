using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB
{
    internal class WalService
    {
        private LockService _locker = null;
        private FileService _dataFile = null;
        private FileService _waFfile = null;
        private Logger _log = null;

        //TODO: change simple hash to TransactionInfo class with DateTime
        private HashSet<Guid> _confirmedTransactions = new HashSet<Guid>();
        private ConcurrentDictionary<uint, ConcurrentDictionary<int, long>> _index = new ConcurrentDictionary<uint, ConcurrentDictionary<int, long>>();

        private int _currentReadVersion = 0;

        public WalService(LockService locker, FileService dataFile, FileService walFile, Logger log)
        {
            _locker = locker;
            _dataFile = dataFile;
            _waFfile = walFile;
            _log = log;
        }

        /// <summary>
        /// Get current read version for all new transactions
        /// </summary>
        public int CurrentReadVersion => _currentReadVersion;

        /// <summary>
        /// Checks if an Page/Version are in WAL-index memory. Consider version that are below parameter. Returns PagePosition of this page inside WAL-file or Empty if page doesn't found.
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
        /// Confirm transaction using new HeaderPage with transaction ID return new current version number
        /// </summary>
        public int Commit(HeaderPage header, IDictionary<uint, PagePosition> pagePositions)
        {
            // add transaction to confirmed list
            _confirmedTransactions.Add(header.TransactionID);

            // write confirmed header page (use First() to execute)
            _waFfile.WritePagesSequence(new BasePage[] { header });

            // must lock commit operation to update WAL-Index (memory only operation)
            lock (_locker)
            {
                // increment current version
                _currentReadVersion++;

                // update wal-index
                foreach (var pos in pagePositions)
                {
                    // get page slot in _index (by pageID) (or create if not exists)
                    var slot = _index.GetOrAdd(pos.Key, new ConcurrentDictionary<int, long>());

                    // add page version (update if already exists)
                    slot.AddOrUpdate(_currentReadVersion, pos.Value.Position, (v, old) => pos.Value.Position);
                }

                return _currentReadVersion;
            }
        }

        /// <summary>
        /// Do WAL checkpoint coping confirmed pages transaction from WAL file to datafile
        /// </summary>
        public void Checkpoint()
        {
            lock (_locker)
            {
                // if walfile are empty, just exit
                if (_waFfile.IsEmpty()) return;

                // must enter in exclusive lock mode
                using (_locker.Exclusive())
                {
                    // if there is not confirmed transaction
                    if (_confirmedTransactions.Count == 0)
                    {
                        // read all file and get only confirmed transaction (with header page in WAL)
                        foreach (var transactionID in _waFfile
                                .ReadAllPages()
                                .Where(x => x.PageType == PageType.Header)
                                .Select(x => x.TransactionID))
                        {
                            _confirmedTransactions.Add(transactionID);
                        }
                    }

                    // if no confirmed transaction, exit
                    if (_confirmedTransactions.Count == 0) return;

                    _log.WalCheckpoint(_confirmedTransactions, _waFfile);

                    // read all pages from WAL that are confirmed
                    // pages are in insert-order, can re-write same pages many times with no problem (only last version will be valid)
                    // clear TransactionID before write on datafile
                    var walpages = _waFfile
                        .ReadAllPages()
                        .Where(x => _confirmedTransactions.Contains(x.TransactionID))
                        .ForEach((i, p) => p.TransactionID = Guid.Empty);

                    // write on datafile (pageID position based) for each wal page
                    _dataFile.WritePages(walpages).Execute();

                    // clear wal-file and wal-index and current version
                    _confirmedTransactions.Clear();
                    _currentReadVersion = 0;
                    _waFfile.Clear();
                    _index.Clear();
                }
            }
        }
    }
}