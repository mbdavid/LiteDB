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
    internal class TransactionService
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

        public TransactionService(HeaderPage header, LockService locker, DataFileService datafile, WalService wal, int maxTransactionSize, Logger log, Action<Guid> done)
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
        public Snapshot CreateSnapshot(LockMode mode, string collection, bool addIfNotExists)
        {
            // if transaction are commited/aborted do not accept new snapshots
            if (this.State == TransactionState.Commited || this.State == TransactionState.Aborted) throw LiteException.InvalidTransactionState("CreateSnapshot", this.State);

            this.State = TransactionState.Active;

            var snapshot = _snapshots.GetOrAdd(collection, c => new Snapshot(mode, collection, _header, _transPages, _locker, _dataFile, _wal));

            DEBUG(snapshot.Mode == LockMode.None, "snaphost always need to be read/write lock here");

            if (mode == LockMode.Write)
            {
                // will create collection if needed only here
                snapshot.WriteMode(addIfNotExists);
            }

            return snapshot;
        }

        /// <summary>
        /// If current transaction contains too much pages, now is safe to remove clean pages from memory and flush to wal disk dirty pages
        /// </summary>
        public void Safepoint()
        {
            // if transaction is in shutting down, stop transaction and do not commit
            if (_shutdown) throw LiteException.DatabaseShutdown();

            // Safepoint are valid only during transaction execution
            DEBUG(this.State != TransactionState.Active, "Safepoint() are called during an invalid transaction state");

            if (_transPages.TransactionSize >= _maxTransactionSize)
            {
                this.PersistDirtyPages(false, false);
            }
        }

        /// <summary>
        /// Persist all dirty in-memory pages (in all snapshots) and clear local pages (even clean pages)
        /// </summary>
        private void PersistDirtyPages(bool includeCollectionPages, bool markLastAsConfirmed)
        {
            // inner method to get all dirty pages
            IEnumerable<BasePage> _()
            {
                // get all dirty pages from all write snapshots
                // can include (or not) collection pages
                var pages = _snapshots.Values
                    .SelectMany(x => x.GetDirtyPages(includeCollectionPages))
                    .ForEach((i, p) => p.TransactionID = this.TransactionID);

                BasePage last = null;

                foreach (var page in pages)
                {
                    if (last != null)
                    {
                        yield return last;
                    }

                    last = page;
                }

                if (last != null)
                {
                    last.IsConfirmed = markLastAsConfirmed;
                    yield return last;
                }
            };

            // get all dirty pages
            var dirtyPages = _();

            // write all pages, in sequence on wal-file and store references into wal pages on transPages
            _wal.WalFile.WritePages(dirtyPages, _transPages.DirtyPagesWal);

            // clear local pages in all snapshots
            foreach (var snapshot in _snapshots.Values)
            {
                snapshot.ClearLocalPages();
            }

            // there is no local pages in cache and all dirty pages are in wal area, clear page count
            _transPages.TransactionSize = 0;
        }

        /// <summary>
        /// Write pages into disk and confirm transaction in wal-index. Returns true if any dirty page was updated
        /// </summary>
        public bool Commit()
        {
            if (this.State == TransactionState.Commited || this.State == TransactionState.Aborted) return false;

            if (this.State == TransactionState.Active)
            {
                // first, check if has any write snapshot
                var hasWriteSnapshot = _snapshots.Values
                    .Where(x => x.Mode == LockMode.Write)
                    .Any();

                if (hasWriteSnapshot)
                {
                    // persist all pages into wal file
                    this.PersistDirtyPages(true, !_transPages.HeaderChanged);

                    if (_transPages.HeaderChanged)
                    {
                        // lock header page to avoid concurrency when writing on header
                        lock (_header)
                        {
                            var newEmptyPageID = _header.FreeEmptyPageID;

                            // if has deleted pages in this transaction, fix FreeEmptyPageID
                            if (_transPages.DeletedPages > 0)
                            {
                                // now, my free list will starts with first page ID
                                newEmptyPageID = _transPages.FirstDeletedPageID;

                                // if free empty list was not empty, let's fix my last page
                                if (_header.FreeEmptyPageID != uint.MaxValue)
                                {
                                    // create new last deleted page from my list to update next linked
                                    var lastDeletedPage = new EmptyPage(_transPages.LastDeletedPageID)
                                    {
                                        // update nextPageID of last deleted page to old first page ID
                                        NextPageID = _header.FreeEmptyPageID,
                                        TransactionID = this.TransactionID,
                                        IsDirty = true
                                    };

                                    // this page will write twice on wal, but no problem, only this last version will be saved on data file
                                    _wal.WalFile.WritePages(new [] { lastDeletedPage }, null);
                                }
                            }

                            // create a header-confirm page based on current header page state (global header are in lock)
                            var header = _header.Clone();

                            // update this confirm page with current transactionID
                            header.UpdateCollections(_transPages);
                            header.FreeEmptyPageID = newEmptyPageID;
                            header.TransactionID = this.TransactionID;

                            // this header page will be masked as confirmed page in WAL file
                            header.IsConfirmed = true;
                            header.IsDirty = true;

                            // persist header in WAL file
                            _wal.WalFile.WritePages(new[] { header }, null);

                            // flush wal file (inside _header lock)
                            _wal.WalFile.Flush();

                            // and update wal-index (before release _header lock)
                            _wal.ConfirmTransaction(this.TransactionID, _transPages.DirtyPagesWal.Values);

                            // update global header page to make equals to confirm page
                            _header.UpdateCollections(_transPages);
                            _header.FreeEmptyPageID = newEmptyPageID;
                        }
                    }
                    else if (_transPages.DirtyPagesWal.Count > 0)
                    {
                        // flush wal file
                        _wal.WalFile.Flush();

                        // and update wal-index 
                        _wal.ConfirmTransaction(this.TransactionID, _transPages.DirtyPagesWal.Values);
                    }
                }

                // dispose all snaps and release locks only after wal index are updated
                foreach (var snapshot in _snapshots.Values)
                {
                    snapshot.Dispose();
                }
            }

            this.Done(TransactionState.Commited);

            return true;
        }

        /// <summary>
        /// Rollback transaction operation - ignore all modified pages and return new pages into disk
        /// </summary>
        public bool Rollback()
        {
            if (this.State == TransactionState.Commited || this.State == TransactionState.Aborted) return false;

            if (this.State == TransactionState.Active)
            {
                // only return pages if transaction has new pages
                if (_transPages.NewPages.Count > 0)
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

            return true;
        }

        /// <summary>
        /// Release transaction as is - it's same as rollback but has no return pages (for new pages) and 
        /// </summary>
        public bool Release()
        {
            if (this.State == TransactionState.Commited || this.State == TransactionState.Aborted) return false;

            if (this.State == TransactionState.Active)
            {
                // dispose all snaps an release locks
                foreach (var snaphost in _snapshots.Values)
                {
                    snaphost.Dispose();
                }
            }

            this.Done(TransactionState.Aborted);

            return true;
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

                pages.Add(new EmptyPage(pageID)
                {
                    NextPageID = next,
                    TransactionID = transactionID,
                    IsDirty = true
                });
            }

            // now lock header to update FreePageList
            lock (_header)
            {
                // fix last page with current header free empty page
                pages.Last().NextPageID = _header.FreeEmptyPageID;

                // persist all empty pages into wal-file
                var pagePositions = new Dictionary<uint, PagePosition>();

                _wal.WalFile.WritePages(pages, pagePositions);

                // create copy of header page to send to wal file
                var header = _header.Clone();

                // update confirm page with my new transaction ID
                header.TransactionID = transactionID;
                header.FreeEmptyPageID = pages.First().PageID;
                header.IsConfirmed = true;
                header.IsDirty = true;

                // write last page (is a header page with confirm checked)
                _wal.WalFile.WritePages(new[] { header }, null);

                // now confirm this transaction to wal
                _wal.ConfirmTransaction(transactionID, pagePositions.Values);

                // and update curret header
                _header.FreeEmptyPageID = header.FreeEmptyPageID;
            }
        }

        /// <summary>
        /// Define this transaction must stop working and release resources because main thread are shutting down.
        /// If was called by same thread, call rollback now (with no ReturnNewPages)
        /// </summary>
        public void Shutdown()
        {
            if (Thread.CurrentThread.ManagedThreadId == this.ThreadID)
            {
                this.Rollback();
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
    }
}