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
    /// Do all WAL index services based on LOG file - has only single instance per engine
    /// [ThreadSafe]
    /// </summary>
    internal class WalIndexService
    {
        private readonly DiskService _disk;
        private readonly LockService _locker;

        private readonly ConcurrentDictionary<uint, List<KeyValuePair<int, long>>> _index = new ConcurrentDictionary<uint, List<KeyValuePair<int, long>>>();

        private readonly HashSet<uint> _confirmTransactions = new HashSet<uint>();

        private int _currentReadVersion = 0;

        /// <summary>
        /// Store last used transaction ID
        /// </summary>
        private int _lastTransactionID = 0;

        public WalIndexService(DiskService disk, LockService locker)
        {
            _disk = disk;
            _locker = locker;
        }

        /// <summary>
        /// Get current read version for all new transactions
        /// </summary>
        public int CurrentReadVersion => _currentReadVersion;

        /// <summary>
        /// Get current counter for transaction ID
        /// </summary>
        public int LastTransactionID => _lastTransactionID;

        /// <summary>
        /// Get new transactionID in thread safe way
        /// </summary>
        public uint NextTransactionID()
        {
            return (uint)Interlocked.Increment(ref _lastTransactionID);
        }

        /// <summary>
        /// Checks if a Page/Version are in WAL-index memory. Consider version that are below parameter. Returns PagePosition of this page inside WAL-file or Empty if page doesn't found.
        /// </summary>
        public long GetPageIndex(uint pageID, int version)
        {
            // wal-index versions must be greater than 0 (version 0 is datafile)
            if (version == 0) return long.MaxValue;

            // get page slot in cache
            if (_index.TryGetValue(pageID, out var list))
            {
                // list are sorted by version number
                var idx = list.Count;
                var position = long.MaxValue;

                // get all page versions in wal-index
                // and then filter only equals-or-less then selected version
                while (idx > 0)
                {
                    idx--;

                    var v = list[idx];

                    if (v.Key <= version)
                    {
                        position = v.Value;
                        break;
                    }
                }

                return position;
            }

            return long.MaxValue;
        }

        /// <summary>
        /// Add transactionID in confirmed list and update WAL index with all pages positions
        /// </summary>
        public void ConfirmTransaction(uint transactionID, ICollection<PagePosition> pagePositions)
        {
            // must lock commit operation to update WAL-Index (memory only operation)
            lock (_index)
            {
                // increment current version
                _currentReadVersion++;

                // update wal-index
                foreach (var pos in pagePositions)
                {
                    // get page slot in _index (by pageID) (or create if not exists)
                    var slot = _index.GetOrAdd(pos.PageID, new List<KeyValuePair<int, long>>());

                    // add version/position into pageID slot
                    slot.Add(new KeyValuePair<int, long>(_currentReadVersion, pos.Position));
                }

                // add transaction as confirmed
                _confirmTransactions.Add(transactionID);
            }
        }

        /// <summary>
        /// Load all confirmed transactions from log file (used only when open datafile)
        /// Don't need lock because it's called on ctor of LiteEngine
        /// </summary>
        public void RestoreIndex(ref HeaderPage header)
        {
            // get all page positions
            var positions = new Dictionary<long, List<PagePosition>>();
            var current = 0L;

            // read all pages to get confirmed transactions (do not read page content, only page header)
            foreach (var buffer in _disk.ReadFull(FileOrigin.Log))
            {
                // read direct from buffer to avoid create BasePage structure
                var pageID = buffer.ReadUInt32(BasePage.P_PAGE_ID);
                var isConfirmed = buffer.ReadBool(BasePage.P_IS_CONFIRMED);
                var transactionID = buffer.ReadUInt32(BasePage.P_TRANSACTION_ID);

                var position = new PagePosition(pageID, current);

                if (positions.TryGetValue(transactionID, out var list))
                {
                    list.Add(position);
                }
                else
                {
                    positions[transactionID] = new List<PagePosition> { position };
                }

                if (isConfirmed)
                {
                    this.ConfirmTransaction(transactionID, positions[transactionID]);

                    var pageType = (PageType)buffer.ReadByte(BasePage.P_PAGE_TYPE);

                    // when a header is modified in transaction, must always be the last page inside log file (per transaction)
                    if (pageType == PageType.Header)
                    {
                        // page buffer instance can't change
                        var headerBuffer = header.Buffer;

                        // copy this buffer block into original header block
                        Buffer.BlockCopy(buffer.Array, buffer.Offset, headerBuffer.Array, headerBuffer.Offset, PAGE_SIZE);

                        // re-load header (using new buffer data)
                        header = new HeaderPage(headerBuffer);
                        header.TransactionID = uint.MaxValue;
                        header.IsConfirmed = false;
                    }
                }

                // update last transaction ID
                _lastTransactionID = (int)transactionID;

                current += PAGE_SIZE;
            }
        }

        /// <summary>
        /// Do checkpoint operation to copy log pages into data file. Return how many transactions was commited inside data file
        /// If soft = true, just try enter in exclusive mode - if not possible, just exit
        /// </summary>
        public void Checkpoint(bool soft)
        {
            // get original log length
            var logLength = _disk.GetLength(FileOrigin.Log);

            // no log file or no confirmed transaction, just exit
            if (logLength == 0 || _confirmTransactions.Count == 0) return;

            // for safe, lock all database (read/write) before run checkpoint operation
            // future versions can be smarter and avoid lock (be more like SQLite checkpoint)
            if (soft)
            {
                // shutdown mode only try enter in exclusive mode... if not possible, exit without checkpoint
                if (_locker.TryEnterExclusive() == false) return;
            }
            else
            {
                _locker.EnterReserved(true);
            }

            LOG($"checkpoint", "WAL");

            // wait all pages write on disk
            _disk.Queue.Wait();

            ENSURE(_disk.Queue.Length == 0, "no pages on queue when checkpoint");

            // getting all "good" pages from log file to be copied into data file
            IEnumerable<PageBuffer> source()
            {
                foreach (var buffer in _disk.ReadFull(FileOrigin.Log))
                {
                    // read direct from buffer to avoid create BasePage structure
                    var transactionID = buffer.ReadUInt32(BasePage.P_TRANSACTION_ID);

                    // only confied paged can be write on data disk
                    if (_confirmTransactions.Contains(transactionID))
                    {
                        var pageID = buffer.ReadUInt32(BasePage.P_PAGE_ID);

                        // clear isConfirmed/transactionID
                        buffer.Write(uint.MaxValue, BasePage.P_TRANSACTION_ID);
                        buffer.Write(false, BasePage.P_IS_CONFIRMED);

                        buffer.Position = BasePage.GetPagePosition(pageID);

                        yield return buffer;
                    }
                }
            }

            // write all log pages into data file (sync)
            _disk.Write(source(), FileOrigin.Data);

            // reset 
            _confirmTransactions.Clear();
            _index.Clear();

            _currentReadVersion = 0;

            // clear cache
            _disk.Cache.Clear();

            // clear log file (sync)
            _disk.SetLength(0, FileOrigin.Log);

            // remove exclusive lock
            _locker.ExitReserved(true);
        }
    }
}