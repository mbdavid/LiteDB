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
        private HeaderPage _header;
        private WalService _wal;
        private LockService _locker;
        private FileService _datafile;
        private Logger _log;

        // event to capture when transaction finish
        internal event EventHandler Done;

        // transaction controls
        private Guid _transactionID = Guid.NewGuid();
        internal TransactionState _state = TransactionState.New;
        private Dictionary<string, Snapshot> _snapshots = new Dictionary<string, Snapshot>(StringComparer.OrdinalIgnoreCase);
        private TransactionPages _transPages = new TransactionPages();

        // transaction info
        public Guid TransactionID => _transactionID;
        public TransactionState State => _state;
        public DateTime StartTime { get; private set; } = DateTime.Now;
        public DateTime DisposeTime { get; private set; } = DateTime.MinValue;

        internal LiteTransaction(HeaderPage header, LockService locker, WalService wal, FileService datafile, Logger log)
        {
            _wal = wal;
            _log = log;

            // retain instances
            _header = header;
            _locker = locker;
            _wal = wal;
            _datafile = datafile;

            // enter transaction locker to avoid 2 transactions in same thread
            _locker.EnterTransaction();
        }

        /// <summary>
        /// Create (or get from cache) snapshot and return. Snapshot are thread-safe. Do not call Dispose of snapshot because transaction will do this on end
        /// </summary>
        internal Snapshot CreateSnapshot(SnapshotMode mode, string collectionName, bool addIfNotExists)
        {
            // lock here only to get ensure that will be 1 request per time
            lock (_snapshots)
            {
                // if transaction are commited/aborted do not accept new snapshots
                if (_state == TransactionState.Aborted || _state == TransactionState.Commited || _state == TransactionState.Disposed) throw LiteException.InvalidTransactionState("CreateSnapshot", _state);

                var snapshot = _snapshots.GetOrAdd(collectionName, c => new Snapshot(mode, collectionName, _header, _transPages, _locker, _wal, _datafile));

                if (mode == SnapshotMode.Write)
                {
                    // will create collection if needed only here
                    snapshot.WriteMode(addIfNotExists);
                }

                _state = TransactionState.InUse;

                return snapshot;
            }
        }

        /// <summary>
        /// Implement a safe point to clear all read-only pages or persist dirty pages (into wal) in all snaps
        /// </summary>
        internal void Safepoint()
        {
            // Safepoint are valid only during transaction execution
            if (_state != TransactionState.InUse) throw LiteException.InvalidTransactionState("Safepoint", _state);

            if (_transPages.PageCount > MAX_PAGES_TRANSACTION)
            {
                // PersistDirtyPages are valid only during transaction execution
                if (_state != TransactionState.InUse) throw LiteException.InvalidTransactionState("Safepoint", _state);

                this.PersistDirtyPages();
            }
        }

        /// <summary>
        /// Persist all dirty in-memory pages (in all snapshots) and clear local pages (even clean pages)
        /// </summary>
        public void PersistDirtyPages()
        {
            // get all pages, in PageID order to be saved on wal (must be in order to avoid checkpoint read wal as normal page)
            var pages = _snapshots.Values
                .SelectMany(x => x.LocalPages.Values)
                .Where(x => x.IsDirty && x.PageID > 0)
                .OrderBy(x => x.PageID)
                .ForEach((i, p) => p.TransactionID = _transactionID)
                //TODO for debug propose - remove on release
                .ToArray();

            // write all pages, in sequence on wal-file and store references into wal pages on transPages
            _datafile.WritePages(pages, false, _transPages.DirtyPagesWal);

            // clear local pages in all snapshots
            foreach (var snapshot in _snapshots.Values)
            {
                snapshot.LocalPages.Clear();
            }

            // there is no local pages in cache and all dirty pages are in wal area, clear page count
            _transPages.PageCount = 0;
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
            lock (_header)
            {
                var newEmptyPageID = _header.FreeEmptyPageID;

                // if has deleted pages in this transaction, fix FreeEmptyPageID
                if (_transPages.DeletedPages > 0)
                {
                    // now, my free list will starts with first page ID
                    newEmptyPageID = _transPages.FirstDeletedPage.PageID;

                    // if free empty list was not empty, let's fix my last page
                    if (_header.FreeEmptyPageID != uint.MaxValue)
                    {
                        // update nextPageID of last deleted page to old first page ID
                        _transPages.LastDeletedPage.NextPageID = _header.FreeEmptyPageID;

                        // this page will write twice on wal, but no problem, only this last version will be saved on data file
                        _datafile.WritePages(new BasePage[] { _transPages.LastDeletedPage }, false, null);
                    }
                }

                // create a header-confirm page based on current header page state (global header are in lock)
                var confirm = _header.Clone() as HeaderPage;

                // update this confirm page with current transactionID
                confirm.Update(_transactionID, newEmptyPageID, _transPages);

                // now, write confirm transaction (with header page) and update wal-index
                _wal.ConfirmTransaction(confirm, _transPages.DirtyPagesWal.Values);

                // update global header page to make equals to confirm page
                _header.Update(Guid.Empty, newEmptyPageID, _transPages);
            }

            // dispose all snaps and release locks only after wal index are updated
            foreach (var snapshot in _snapshots.Values)
            {
                snapshot.Dispose();
            }

            _state = TransactionState.Commited;
        }

        /// <summary>
        /// Rollback transaction operation - ignore all modified pages and return new pages into disk
        /// </summary>
        public void Rollback()
        {
            if (_state == TransactionState.New || _state == TransactionState.Aborted) return;
            if (_state != TransactionState.InUse) throw LiteException.InvalidTransactionState("Rollback", _state);

            // if this aborted transaction request new pages, create new transaction do return this pages
            if (_transPages.NewPages.Count > 0)
            {
                this.ReturnNewPages();
            }

            // dispose all snaps an release locks
            foreach (var snaphost in _snapshots.Values)
            {
                snaphost.Dispose();
            }

            _state = TransactionState.Aborted;
        }

        /// <summary>
        /// Return added pages when occurs an rollback transaction (run this only in rollback). Create new transactionID and add into
        /// WAL file all new pages as EmptyPage in a linked order - also, update SharedPage before store
        /// </summary>
        public void ReturnNewPages()
        {
            var pages = new List<EmptyPage>();

            // create new transaction ID
            var transactionID = Guid.NewGuid();

            // create list of empty pages with forward link pointer
            for (var i = 0; i < _transPages.NewPages.Count; i++)
            {
                var pageID = _transPages.NewPages[i];
                var next = i < _transPages.NewPages.Count - 1 ? _transPages.NewPages[i + 1] : uint.MaxValue;
                var prev = i > 0 ? _transPages.NewPages[i - 1] : 0;

                pages.Add(new EmptyPage(pageID)
                {
                    NextPageID = next,
                    PrevPageID = prev,
                    TransactionID = transactionID,
                    IsDirty = true
                });
            }

            // now lock header to update FreePageList
            lock (_header)
            {
                // fix last page with current header free empty page
                pages.Last().NextPageID = _header.FreeEmptyPageID;

                // create copy of header page to send to wal file
                var confirm = _header.Clone() as HeaderPage;

                _header.Update(transactionID, pages.First().PageID, null);

                // persist all pages into wal-file (new run ToList now)
                var pagePositions = new Dictionary<uint, PagePosition>();

                _datafile.WritePages(pages, false, pagePositions);

                // now commit last confirm page to wal file
                _wal.ConfirmTransaction(confirm, pagePositions.Values);

                // now can update global header version
                _header.Update(Guid.Empty, confirm.FreeEmptyPageID, null);
            }
        }

        public void Dispose()
        {
            if (_state == TransactionState.Disposed) return;

            // if no commit/rollback are invoke before dipose, let's rollback by default
            if (_state == TransactionState.InUse)
            {
                this.Rollback();
            }

            // dispose transactio state and date time
            _state = TransactionState.Disposed;

            this.DisposeTime = DateTime.Now;

            _locker.ExitTransaction();

            // call dispose event
            if (this.Done != null)
            {
                this.Done(this, EventArgs.Empty);
            }
        }
    }
}