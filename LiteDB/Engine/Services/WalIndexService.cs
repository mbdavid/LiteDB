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

        private readonly ConcurrentDictionary<uint, bool> _transactions = new ConcurrentDictionary<uint, bool>();
        private readonly ConcurrentDictionary<uint, List<KeyValuePair<int, long>>> _index = new ConcurrentDictionary<uint, List<KeyValuePair<int, long>>>();

        private readonly object _checkpointLocker = new object();

        private int _currentReadVersion = 0;

        public WalIndexService(DiskService disk, LockService locker)
        {
            _disk = disk;
            _locker = locker;

            // when writer queue flush some transaction, confirm in my transaction list (or add)
            _disk.Flush += (transactionID) =>
            {
                _transactions.AddOrUpdate(transactionID, true, (t, o) => true);
            };
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
            // if this transaction was no persisted yet (by async writer) create as not persisted yet
            _transactions.TryAdd(transactionID, false);

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
                    var slot = _index.GetOrAdd(pos.PageID, new List<KeyValuePair<int, long>>());

                    // add version/position into pageID slot
                    slot.Add(new KeyValuePair<int, long>(_currentReadVersion, pos.Position));
                }
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
                // check if page is ok
                var page = new BasePage(buffer);
                var crc = buffer.ComputeChecksum();
                
                if (page.CRC != crc)
                {
                    throw new LiteException(0, $"Invalid checksum (CRC) in log file on position {current}");
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
                        var headerBuffer = header.Buffer;

                        // copy this buffer block into original header block
                        Buffer.BlockCopy(buffer.Array, buffer.Offset, headerBuffer.Array, headerBuffer.Offset, PAGE_SIZE);

                        // re-load header (using new buffer data)
                        header = new HeaderPage(headerBuffer);
                        header.TransactionID = uint.MaxValue;
                        header.IsConfirmed = false;
                        header.LastTransactionID = (int)page.TransactionID;
                    }

                    // mark transaction as persisted
                    _transactions[page.TransactionID] = true;
                }

                current += PAGE_SIZE;
            }
        }

        /// <summary>
        /// Do checkpoint operation to copy log pages into data file. Return how many transactions was commited inside data file
        /// </summary>
        public int Checkpoint(HeaderPage header, CheckpointMode mode)
        {
            // ensure only 1 thread can run checkpoint
            lock (_checkpointLocker)
            {
                // get original log length
                var logLength = _disk.GetLength(FileOrigin.Log);
                var counter = 0;
                var locked = false;

                // no log file, just exit
                if (logLength == 0)
                {
                    if (mode == CheckpointMode.Shutdown) _disk.Delete(FileOrigin.Log);

                    return 0;
                }

                LOG($"{mode.ToString().ToLower()} checkpoint", "WAL");

                // for full/shutdown checkpoint need exclusive lock
                if (mode == CheckpointMode.Full || mode == CheckpointMode.Shutdown)
                {
                    _locker.EnterReserved(true);

                    // wait all async writer queue runs
                    _disk.Queue.Wait();

                    ENSURE(_transactions.Where(x => x.Value == false).Count() == 0, "should not have any pending transaction after queue wait");

                    locked = true;
                }

                // get only flushed transactions (need run before starts)
                var confirmTransactions = new HashSet<uint>(_transactions.Where(x => x.Value).Select(x => x.Key));

                // getting all "good" pages from log file to be copied into data file
                IEnumerable<PageBuffer> source()
                {
                    foreach (var buffer in _disk.ReadFull(FileOrigin.Log))
                    {
                        var page = new BasePage(buffer);

                        // only confied paged can be write on data disk
                        if (confirmTransactions.Contains(page.TransactionID))
                        {
                            buffer.Position = BasePage.GetPagePosition(page.PageID);

                            var isConfirmed = page.IsConfirmed;
                            var transactionID = page.TransactionID;

                            // clean log-only data
                            page.IsConfirmed = false;
                            page.TransactionID = uint.MaxValue;

                            // get updated buffer with new CRC computed
                            yield return page.UpdateBuffer();

                            // can remove from list when store in disk
                            if (isConfirmed)
                            {
                                counter++;
                                _transactions.TryRemove(transactionID, out var d);
                            }
                        }
                    }

                    // return header page as last checkpoint page
                    header.LastCheckpoint = DateTime.UtcNow;

                    var headerBuffer = header.UpdateBuffer();

                    yield return headerBuffer;
                }

                // write all log pages into data file (sync)
                _disk.Write(source(), FileOrigin.Data);

                // check if is possible clear log file (no pending transaction and no new log pages)
                if (_transactions.Count == 0 && logLength == _disk.GetLength(FileOrigin.Log))
                {
                    // enter exclusive mode (if not yet)
                    if (locked == false) _locker.EnterReserved(true);

                    // double check because lock waiter can be executed a write operation
                    if (_transactions.Count == 0 && logLength == _disk.GetLength(FileOrigin.Log))
                    {
                        _currentReadVersion = 0;
                        _index.Clear();

                        // clear log file (sync)
                        _disk.SetLength(0, FileOrigin.Log);
                    }

                    if (locked == false) _locker.ExitReserved(true);
                }

                // in shutdown mode, delete file (will Dispose Stream) - will delete only if file is complete empty
                if (mode == CheckpointMode.Shutdown)
                {
                    _disk.Delete(FileOrigin.Log);
                }

                ENSURE(mode == CheckpointMode.Full, _disk.GetLength(FileOrigin.Log) == 0, "full checkpoint must finish with log file = 0");
                ENSURE(mode == CheckpointMode.Shutdown, _disk.GetLength(FileOrigin.Log) == 0, "shutdown checkpoint must finish with log file = 0");

                // exit exclusive lock (if full/shutdown)
                if (locked) _locker.ExitReserved(true);

                return counter;
            }
        }
    }
}