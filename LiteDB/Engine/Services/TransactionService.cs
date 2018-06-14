using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represent a single transaction service. Need a new instance for each transaction.
    /// You must run each transaction in a different thread - no 2 transaction in same thread (locks as per-thread)
    /// </summary>
    internal class TransactionService : IDisposable
    {
        // instances from Engine
        private readonly HeaderPage _header;
        private readonly LockService _locker;
        private readonly DataFileService _dataFile;
        private readonly WalService _wal;
        private readonly Logger _log;

        // transaction controls
        private readonly Dictionary<string, Snapshot> _snapshots = new Dictionary<string, Snapshot>(StringComparer.OrdinalIgnoreCase);
        private readonly TransactionPages _transPages = new TransactionPages();
        private readonly Action<Guid> _done;
        private readonly int _maxTransactionSize;
        private bool _shutdown = false;

        // transaction info
        public int ThreadID { get; private set; } = Thread.CurrentThread.ManagedThreadId;
        public Guid TransactionID { get; private set; } = Guid.NewGuid();
        public TransactionState State { get; private set; } = TransactionState.New;
        public DateTime StartTime { get; private set; } = DateTime.Now;
        public Dictionary<string, Snapshot> Snapshots => _snapshots;
        public TransactionPages Pages => _transPages;

        internal TransactionService(HeaderPage header, LockService locker, DataFileService datafile, WalService wal, int maxTransactionSize, Logger log, Action<Guid> done)
        {
            _wal = wal;
            _log = log;
            _done = done;
            _maxTransactionSize = maxTransactionSize;

            // retain instances
            _header = header;
            _locker = locker;
            _dataFile = datafile;
            _wal = wal;

            // enter transaction locker to avoid 2 transactions in same thread
            _locker.EnterTransaction();
        }

        /// <summary>
        /// Create (or get from cache) snapshot and return. Snapshot are thread-safe. Do not call Dispose of snapshot because transaction will do this on end
        /// </summary>
        internal Snapshot CreateSnapshot(SnapshotMode mode, string collectionName, bool addIfNotExists)
        {
            // if transaction are commited/aborted do not accept new snapshots
            if (this.State == TransactionState.Commited || this.State == TransactionState.Aborted) throw LiteException.InvalidTransactionState("CreateSnapshot", this.State);

            this.State = TransactionState.Active;

            var snapshot = _snapshots.GetOrAdd(collectionName, c => new Snapshot(mode, collectionName, _header, _transPages, _locker, _dataFile, _wal));

            if (mode == SnapshotMode.Write)
            {
                // will create collection if needed only here
                snapshot.WriteMode(addIfNotExists);
            }

            return snapshot;
        }

        /// <summary>
        /// If current transaction contains too much pages, now is safe to remove clean pages from memory and flush to wal disk dirty pages
        /// </summary>
        internal void Safepoint()
        {
            // transaction is in shutdown (do rollback)
            if (_shutdown) throw LiteException.DatabaseShutdown();

            // Safepoint are valid only during transaction execution
            DEBUG(this.State != TransactionState.Active, "Safepoint() are called during an invalid transaction state");

            if (_transPages.TransactionSize >= _maxTransactionSize)
            {
                this.PersistDirtyPages();
            }
        }

        /// <summary>
        /// Persist all dirty in-memory pages (in all snapshots) and clear local pages (even clean pages)
        /// </summary>
        internal void PersistDirtyPages()
        {
            // get all dirty pages from write snapshots
            // do not get header page because will use as confirm page (last page)
            // update if transactionID
            var pages = _snapshots.Values
                .Where(x => x.Mode == SnapshotMode.Write)
                .SelectMany(x => x.LocalPages.Values)
                .Where(x => x.IsDirty && x.PageType != PageType.Header)
                .ForEach((i, p) => p.TransactionID = this.TransactionID)
#if DEBUG
                .ToArray(); // for better debug propose
#else
                ;
#endif

            // write all pages, in sequence on wal-file and store references into wal pages on transPages
            _wal.WalFile.WriteAsyncPages(pages, _transPages.DirtyPagesWal);

            // clear local pages in all snapshots
            foreach (var snapshot in _snapshots.Values)
            {
                // clear because I will not use anymore in this transaction
                snapshot.LocalPages.Clear();
            }

            // there is no local pages in cache and all dirty pages are in wal area, clear page count
            _transPages.TransactionSize = 0;
        }

        /// <summary>
        /// Write pages into disk and confirm transaction in wal-index
        /// </summary>
        public void Commit()
        {
            if (this.State == TransactionState.Commited || this.State == TransactionState.Aborted) throw LiteException.InvalidTransactionState("Commit", this.State);

            if (this.State == TransactionState.Active)
            {
                // first, check if has any write snap
                var writeSnaps = _snapshots.Values
                    .Where(x => x.Mode == SnapshotMode.Write)
                    .Any();

                if (writeSnaps)
                {
                    // persist all pages into wal file
                    this.PersistDirtyPages();

                    // if no dirty page, just skip
                    if (_transPages.DirtyPagesWal.Count > 0)
                    {
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
                                    _wal.WalFile.WriteAsyncPages(new BasePage[] { _transPages.LastDeletedPage }, null);
                                }
                            }

                            // create a header-confirm page based on current header page state (global header are in lock)
                            var confirm = _header.Clone() as HeaderPage;

                            // update this confirm page with current transactionID
                            confirm.Update(this.TransactionID, newEmptyPageID, _transPages);

                            // now, write confirm transaction (with header page) and update wal-index
                            _wal.ConfirmTransaction(confirm, _transPages.DirtyPagesWal.Values);

                            // update global header page to make equals to confirm page
                            _header.Update(Guid.Empty, newEmptyPageID, _transPages);
                        }
                    }
                }

                // dispose all snaps and release locks only after wal index are updated
                foreach (var snapshot in _snapshots.Values)
                {
                    snapshot.Dispose();
                }
            }

            this.Done(TransactionState.Commited);
        }

        /// <summary>
        /// Rollback transaction operation - ignore all modified pages and return new pages into disk
        /// </summary>
        public void Rollback(bool returnPages)
        {
            if (this.State == TransactionState.Commited || this.State == TransactionState.Aborted) throw LiteException.InvalidTransactionState("Commit", this.State);

            if (this.State == TransactionState.Active)
            {
                // if this aborted transaction requested for new pages, create new transaction do return this pages do database (as EmptyList pages)
                // this returnPages are optional because TempDB can "loose" this pages
                if (returnPages && _transPages.NewPages.Count > 0)
                {
                    this.ReturnNewPages();
                }

                // dispose all snaps an release locks
                foreach (var snaphost in _snapshots.Values)
                {
                    snaphost.Dispose();
                }
            }

            this.Done(TransactionState.Aborted);
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

                // update confirm page with my new transaction ID
                confirm.TransactionID = transactionID;

                _header.Update(transactionID, pages.First().PageID, null);

                // persist all pages into wal-file (new run ToList now)
                var pagePositions = new Dictionary<uint, PagePosition>();

                _wal.WalFile.WriteAsyncPages(pages, pagePositions);

                // now commit last confirm page to wal file
                _wal.ConfirmTransaction(confirm, pagePositions.Values);

                // now can update global header version
                _header.Update(Guid.Empty, confirm.FreeEmptyPageID, null);
            }
        }

        /// <summary>
        /// Define this transaction must stop working and release resources becase main thread are shutdowing.
        /// If was called by same thread, call rollback now
        /// </summary>
        internal void Shutdown()
        {
            if (Thread.CurrentThread.ManagedThreadId == this.ThreadID)
            {
                this.Rollback(true);
            }
            else
            {
                _shutdown = true;
            }
        }

        /// <summary>
        /// Finish transaction, release lock and call done action
        /// </summary>
        private void Done(TransactionState state)
        {
            this.State = state;

            // release thread transaction lock
            _locker.ExitTransaction();

            // call done
            _done(this.TransactionID);
        }

        public void Dispose()
        {
            if (this.State == TransactionState.New || this.State == TransactionState.Active)
            {
                this.Rollback(true);
            }
        }
    }
}