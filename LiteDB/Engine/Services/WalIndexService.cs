using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// [ThreadSafe]
    /// </summary>
    internal class WalIndexService
    {
        private readonly LockService _locker;

        private readonly List<long> _confirmedTransactions = new List<long>();
        private readonly ConcurrentDictionary<uint, ConcurrentDictionary<int, long>> _index = new ConcurrentDictionary<uint, ConcurrentDictionary<int, long>>();

        private int _currentReadVersion = 0;

        public WalIndexService(LockService locker)
        {
            _locker = locker;
        }

        /// <summary>
        /// Get all confirmed transactions inside log file
        /// </summary>
        public IReadOnlyList<long> ConfirmedTransactions => _confirmedTransactions;

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
        /// Clear all wal index references - used after full checkpoint
        /// </summary>
        public void Clear()
        {
            _confirmedTransactions.Clear();
            _index.Clear();
            _currentReadVersion = 0;
        }

        /// <summary>
        /// Load all confirmed transactions from log file (used only when open datafile)
        /// Don't need lock because it's called on ctor of LiteEngine
        /// </summary>
        public void RestoreIndex(DiskService disk, ref HeaderPage header)
        {
            // get all page positions
            var positions = new Dictionary<long, List<PagePosition>>();
            var current = 0L;

            // read all pages to get confirmed transactions (do not read page content, only page header)
            foreach(var buffer in disk.ReadFull(FileOrigin.Log))
            {
                var page = new BasePage(buffer);

                // check if page is ok
                var crc = page.ComputeChecksum();

                if (page.CRC != crc)
                {
                    throw new LiteException(0, $"Invalid chechsum (CRC) in position {current}");
                }

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

                    // when a header is modified in transaction, must always be the last page inside log file (per transaction)
                    if (page.PageType == PageType.Header)
                    {
                        var headerBuffer = header.GetBuffer(false);

                        // copy this buffer block into original header block
                        Buffer.BlockCopy(buffer.Array, buffer.Offset, headerBuffer.Array, headerBuffer.Offset, PAGE_SIZE);

                        // re-load header (using same buffer)
                        header = new HeaderPage(headerBuffer);
                        header.TransactionID = 0;
                        header.IsConfirmed = false;
                    }
                }

                current += PAGE_SIZE;
            }
        }
    }
}