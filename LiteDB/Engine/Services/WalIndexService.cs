using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public enum CheckpointMode { Full, Shutdown, Incremental }

    /// <summary>
    /// [ThreadSafe]
    /// </summary>
    internal class WalIndexService
    {
        private readonly DiskService _disk;
        private readonly LockService _locker;

        private readonly ConcurrentDictionary<long, bool> _transactions = new ConcurrentDictionary<long, bool>();
        private readonly ConcurrentDictionary<uint, ConcurrentDictionary<int, long>> _index = new ConcurrentDictionary<uint, ConcurrentDictionary<int, long>>();

        private int _currentReadVersion = 0;

        public WalIndexService(DiskService disk, LockService locker)
        {
            _disk = disk;
            _locker = locker;

            // when writer queue flush some transaction, confirm in my transaction list (or add)
            disk.Flush += (transactionID) =>
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
        public void RestoreIndex(ref HeaderPage header)
        {
            // get all page positions
            var positions = new Dictionary<long, List<PagePosition>>();
            var current = 0L;

            // read all pages to get confirmed transactions (do not read page content, only page header)
            foreach(var buffer in _disk.ReadFull(FileOrigin.Log))
            {
                var page = new BasePage(buffer);

                // check if page is ok
                var crc = page.ComputeChecksum();

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
                        var headerBuffer = header.GetBuffer(false);

                        // copy this buffer block into original header block
                        Buffer.BlockCopy(buffer.Array, buffer.Offset, headerBuffer.Array, headerBuffer.Offset, PAGE_SIZE);

                        // re-load header (using new buffer data)
                        header = new HeaderPage(headerBuffer);
                        header.TransactionID = 0;
                        header.IsConfirmed = false;
                    }
                }

                current += PAGE_SIZE;
            }
        }

        /// <summary>
        /// Do checkpoint operation to copy log pages into data file. Return how many transactions was commited inside data file
        /// </summary>
        public int Checkpoint(HeaderPage header, CheckpointMode mode)
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

            LOG($"checkpoint: {mode}", "WAL");

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
            var confirmTransactions = new HashSet<long>(_transactions.Where(x => x.Value).Select(x => x.Key));

            IEnumerable<PageBuffer> source()
            {
                foreach(var buffer in _disk.ReadFull(FileOrigin.Log))
                {
                    var page = new BasePage(buffer);

                    if (confirmTransactions.Contains(page.TransactionID))
                    {
                        buffer.Position = BasePage.GetPagePosition(page.PageID);

                        yield return buffer;

                        // can remove from list when store in disk
                        if (page.IsConfirmed)
                        {
                            counter++;
                            _transactions.TryRemove(page.TransactionID, out var d);
                        }
                    }
                }

                // return header page as last checkpoint page
                header.LastCheckpoint = DateTime.UtcNow;

                yield return header.GetBuffer(true);
            }

            // write all log pages into data file (sync)
            _disk.Write(source(), FileOrigin.Data);

            // check if is possible clear log file (no pending transactio and no new log pages)
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