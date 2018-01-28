using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Represent a single transaction service. Need a new instance for each transaction
    /// </summary>
    public class LiteTransaction : IDisposable
    {
        // if local pages recah this limit, flush to wal disk transaction
        private const int MAX_PAGES_TRANSACTION = 1000;

        // instances from Engine
        internal HeaderPage _header;
        internal WalService _wal;
        internal LockService _locker;
        internal FileService _dataFile;
        internal FileService _walFile;
        internal Logger _log;

        // transaction controls
        private Guid _transactionID = Guid.NewGuid();
        private TransactionState _state = TransactionState.New;
        private Dictionary<string, Snapshot> _snapshots = new Dictionary<string, Snapshot>(StringComparer.OrdinalIgnoreCase);
        private TransactionPages _transPages = new TransactionPages();

        internal LiteTransaction(HeaderPage header, LockService locker, WalService wal, FileService dataFile, FileService walFile, Logger log)
        {
            _wal = wal;
            _log = log;

            // retain instances
            _header = header;
            _locker = locker;
            _wal = wal;
            _dataFile = dataFile;
            _walFile = walFile;
        }

        /// <summary>
        /// Create new (or get already created) snapshot. This process are not thread-safe, so NO 2 snaps from same transaction at same time
        /// </summary>
        internal T CreateSnapshot<T>(SnapshotMode mode, string collectionName, bool addIfNotExists, Func<Snapshot, T> fn)
        {
            // if transaction are commited/aborted do not accept new snapshots
            if (_state == TransactionState.Aborted || _state == TransactionState.Commited) throw LiteException.InvalidTransactionState("CreateSnapshot", _state);

            lock(_snapshots)
            {
                var snapshot = _snapshots.GetOrAdd(collectionName, c => new Snapshot(collectionName, _header, _transPages, _locker, _wal, _dataFile, _walFile));

                if (mode == SnapshotMode.Write)
                {
                    snapshot.WriteMode(addIfNotExists);
                }

                try
                {
                    _state = TransactionState.InUse;

                    var result = fn(snapshot);

                    return result;
                }
                catch
                {
                    // must rollback transaction because dirty pages are not valid
                    this.Rollback();
                    throw;
                }

            }
        }

        /// <summary>
        /// Implement a safe point to clear all read-only pages or persist dirty pages (into wal) in all snaps
        /// </summary>
        internal void Safepoint()
        {
            if (_transPages.PageCount > MAX_PAGES_TRANSACTION)
            {
                this.PersistDirtyPages();

                _transPages.PageCount = 0;
            }
        }

        /// <summary>
        /// Persist all in-memory pages in all snapshots
        /// </summary>
        private void PersistDirtyPages()
        {
            foreach (var snapshot in _snapshots.Values)
            {
                // set all dirty pages with my transactionID
                var dirty = snapshot.LocalPages.Values
                    .Where(x => x.IsDirty)
                    .ForEach((i, p) => p.TransactionID = _transactionID);

                // write all pages, in sequence on wal-file
                _walFile.WritePages(dirty, false, snapshot.DirtyPagesWal);

                // clear local pages
                snapshot.LocalPages.Clear();
            }
        }

        /// <summary>
        /// Write pages into disk and confirm transaction in wal-index
        /// </summary>
        public void Commit()
        {
            if (_state == TransactionState.New || _state == TransactionState.Commited) return;
            if (_state != TransactionState.InUse) throw LiteException.InvalidTransactionState("Commit", _state);

            // persist all pages into wal file
            this.PersistDirtyPages();

            // lock header page to avoid concurrency when writing on header
            lock(_header)
            {
                var newEmptyPageID = _header.FreeEmptyPageID;

                // if has deleted pages in this transaction, fix FreeEmptyPageID
                if (_transPages.HasDeletedPages)
                {
                    _transPages.LastDeletedPage.NextPageID = _header.FreeEmptyPageID;

                    newEmptyPageID = _transPages.FirstDeletedPage.PageID;

                    // if last page was modified, lets save on wal
                    if (_header.FreeEmptyPageID != uint.MaxValue)
                    {
                        // this page will write twice on wal, but no problem, only this last version will be saved on data file
                        _walFile.WritePages(new BasePage[] { _transPages.LastDeletedPage }, false, null);
                    }
                }

                // create a header-confirm page based on current header page state
                var confirm = _header.Copy(_transactionID, newEmptyPageID);

                // create a single list of page position from wal file of this pages
                var pagePositions = new List<PagePosition>();

                foreach (var snapshot in _snapshots.Values)
                {
                    pagePositions.AddRange(snapshot.DirtyPagesWal.Values);
                }

                // now, write confirm transaction (with header page) and update wal-index
                _wal.ConfirmTransaction(confirm, pagePositions);

                // if has deleted pages, update in global header instance
                _header.FreeEmptyPageID = newEmptyPageID;
            }

            // dispose all snaps and release locks only after wal index are updated
            foreach (var snaphost in _snapshots.Values)
            {
                snaphost.Dispose();
            }

            _state = TransactionState.Commited;
        }

        /// <summary>
        /// Rollback transaction operation - ignore all modified pages
        /// </summary>
        public void Rollback()
        {
            if (_state == TransactionState.New || _state == TransactionState.Aborted) return;
            if (_state != TransactionState.InUse) throw LiteException.InvalidTransactionState("Rollback", _state);

            // TODO: DO ROLLBACK of new pages (adding new transaction)

            // dispose all snaps an release locks
            foreach (var snaphost in _snapshots.Values)
            {
                snaphost.Dispose();
            }

            _state = TransactionState.Aborted;
        }

        public void Dispose()
        {
            // if no commit/rollback are invoke before dipose, let's rollback by default
            if (_state == TransactionState.InUse)
            {
                this.Rollback();
            }
        }
    }
}