using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    internal class WalService
    {
        private LockService _locker;
        private FileService _datafile;
        private Logger _log;

        private Dictionary<Guid, HeaderPage> _confirmedTransactions = new Dictionary<Guid, HeaderPage>();
        private ConcurrentDictionary<uint, ConcurrentDictionary<int, long>> _index = new ConcurrentDictionary<uint, ConcurrentDictionary<int, long>>();

        private int _currentReadVersion = 0;

        public WalService(LockService locker, FileService datafile, Logger log)
        {
            _locker = locker;
            _datafile = datafile;
            _log = log;

            this.LoadConfirmedTransactions();
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
        /// Write last confirmation page into all and update all indexes
        /// </summary>
        public void ConfirmTransaction(HeaderPage confirm, IEnumerable<PagePosition> pagePositions)
        {
            // mark confirm page as dirty
            confirm.IsDirty = true;

            // write header-confirm transaction page in wal file
            _datafile.WriteAsyncPages(new HeaderPage[] { confirm }, null);

            // add confirm page into confirmed-queue to be used in checkpoint
            _confirmedTransactions.Add(confirm.TransactionID, confirm);

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
        /// Do WAL checkpoint coping confirmed pages transaction from WAL file to datafile. Return how many pages was copied from WAL area to data area
        /// </summary>
        public int Checkpoint()
        {
            // checkpoint can run only without any open transaction in current thread
            if (_locker.IsInTransaction) throw LiteException.InvalidTransactionState("Checkpoint", TransactionState.InUse);

            // enter in special database reserved lock
            // only new readers are allowed and no writers
            _locker.EnterReserved();

            // before checkpoint, write all async pages in disk
            _datafile.WaitAsyncWrite();

            try
            {
                // get header from disk (not current header)
                var header = _datafile.ReadPage(0, true) as HeaderPage;

                // get all valid wal pages
                var walPages = this.GetWalPages(header);

                // and write on disk in sync mode
                var count = _datafile.WriteWalPages(header, walPages);

                // clear indexes/confirmed transactions
                _index.Clear();
                _confirmedTransactions.Clear();
                _currentReadVersion = 0;

                return count;
            }
            finally
            {
                _locker.ExitReserved();
            }
        }

        /// <summary>
        /// Read datafile and get all valid pages on wal area
        /// </summary>
        private IEnumerable<BasePage> GetWalPages(HeaderPage header)
        {
            // get first page position in wal and datafile length
            var position = BasePage.GetPagePosition(header.LastPageID + 1);
            var length = _datafile.Length;

            // if my position are afer datafile, there is no wal
            if (position >= length) yield break;

            while (position < length)
            {
                var page = _datafile.ReadPage(position, false);

                position += BasePage.PAGE_SIZE;

#if DEBUG
                // WARNING: there is no page on WAL with empty TransactionID
                if (page.TransactionID == Guid.Empty) System.Diagnostics.Debugger.Break();
#endif

                // continue only if page are in confirm transaction list
                if (!_confirmedTransactions.TryGetValue(page.TransactionID, out var confirm)) continue;

                // clear transactionID before write on disk 
                page.TransactionID = Guid.Empty;

                // if page is a confirm header, let's update checkpoint time/counter
                if (page.PageType == PageType.Header)
                {
                    var lastHeader = page as HeaderPage;

                    lastHeader.CheckpointCounter = header.CheckpointCounter + 1;
                    lastHeader.LastCheckpoint = DateTime.Now;
                }

                yield return page;
            }
        }

        /// <summary>
        /// Load all confirmed transactions from datafile (used only when open datafile)
        /// </summary>
        private void LoadConfirmedTransactions()
        {
            // get header from disk
            var header = _datafile.ReadPage(0, false) as HeaderPage;

            // get first page position in wal and datafile length
            var position = BasePage.GetPagePosition(header.LastPageID + 1);
            var length = _datafile.Length;

            // read all wal area to look for confirm HeaderPages
            while(position < length)
            {
                var page = _datafile.ReadPage(position, false);

                position += BasePage.PAGE_SIZE;

                if (page.PageID == 0)
                {
                    _confirmedTransactions.Add(page.TransactionID, page as HeaderPage);
                }
            }
        }
    }
}