using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal class WalIndexService
    {
        private readonly LockService _locker;

        private readonly List<long> _confirmedTransactions = new List<long>();
        private readonly ConcurrentDictionary<uint, ConcurrentDictionary<int, long>> _index = new ConcurrentDictionary<uint, ConcurrentDictionary<int, long>>();

        private int _currentReadVersion = 0;

//**        public LogFileService LogFile => _logFile;
//**        public ConcurrentDictionary<uint, ConcurrentDictionary<int, long>> Index => _index;
//**        public List<long> ConfirmedTransactions => _confirmedTransactions;

        public WalIndexService(LockService locker)
        {
            _locker = locker;
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
        public void ConfirmTransaction(long transactionID, ICollection<PagePosition> pagePositions)
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
        /// Load all confirmed transactions from log file (used only when open datafile)
        /// Don't need lock because it's called on ctor of LiteEngine
        /// </summary>
        public void RestoreIndex(DiskService disk, ref HeaderPage header)
        {
            // leio as paginas SEM o MemoryFile (é mais eficiente pois vou ver poucos dados)
            // uso apenas 1 buffer[8k] de temp + 1 buffer[8k] ultima header
            // a header deve ser sobregravada (o buffer) com uma mais recente
            // sobregravar leia-se buffercopy na pagePage

            throw new NotImplementedException();
            /*
            // get all page positions
            var positions = new Dictionary<ObjectId, List<PagePosition>>();
            var current = 0L;

            // read all pages to get confirmed transactions (do not read page content, only page header)
            foreach(var page in _logFile.ReadPages(false))
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
                        header = _logFile.ReadPage(current) as HeaderPage;
                        header.TransactionID = 0;
                        header.IsConfirmed = false;
                    }
                }

                current += PAGE_SIZE;
            }
            */
        }
    }
}