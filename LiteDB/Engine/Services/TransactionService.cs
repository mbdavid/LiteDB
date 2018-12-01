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
        private readonly DiskService _disk;
        private readonly DiskReader _reader;
        private readonly WalIndexService _walIndex;

        // transaction controls
        private readonly Dictionary<string, Snapshot> _snapshots = new Dictionary<string, Snapshot>(StringComparer.OrdinalIgnoreCase);
        private readonly TransactionPages _transPages = new TransactionPages();
        private readonly Action<long> _done;
        private bool _shutdown = false;

        // transaction info
        public int ThreadID { get; } = Thread.CurrentThread.ManagedThreadId;
        public long TransactionID { get; } = System.Diagnostics.Stopwatch.GetTimestamp();
        public TransactionState State { get; private set; } = TransactionState.New;
        //**public Dictionary<string, Snapshot> Snapshots => _snapshots;
        //**public TransactionPages Pages => _transPages;

        public TransactionService(HeaderPage header, LockService locker, DiskService disk, WalIndexService walIndex, Action<long> done)
        {
            // retain instances
            _header = header;
            _locker = locker;
            _disk = disk;
            _walIndex = walIndex;
            _done = done;

            _reader = _disk.GetReader();

            // enter transaction locker to avoid 2 transactions in same thread
            _locker.EnterTransaction();
        }

        /// <summary>
        /// Create (or get from transaction-cache) snapshot and return
        /// </summary>
        public Snapshot CreateSnapshot(LockMode mode, string collection, bool addIfNotExists)
        {
            // if transaction are commited/aborted do not accept new snapshots
            if (this.State == TransactionState.Commited || this.State == TransactionState.Aborted) throw LiteException.InvalidTransactionState("CreateSnapshot", this.State);

            this.State = TransactionState.Active;

            Snapshot create() => new Snapshot(mode, collection, _header, _transPages, _locker, _walIndex, _reader, addIfNotExists);

            if (_snapshots.TryGetValue(collection, out var snapshot))
            {
                // if current snapshot are ReadOnly but request is about Write mode, dispose read and re-create new in WriteMode
                if (mode == LockMode.Write && snapshot.Mode == LockMode.Read)
                {
                    // dispose current read-only snapshot
                    snapshot.Dispose();

                    // create new snapshot with write mode
                    _snapshots[collection] = snapshot = create();
                }
            }
            else
            {
                // if not exits, let's create here
                _snapshots[collection] = snapshot = create();
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
            ENSURE(this.State == TransactionState.Active, "Safepoint() are called during an invalid transaction state");

            if (_transPages.TransactionSize >= MAX_TRANSACTION_SIZE)
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
            IEnumerable<PageBuffer> source()
            {
                // get all dirty pages from all write snapshots
                // can include (or not) collection pages
                // update DirtyPagesLog inside transPage for all dirty pages was write on disk
                var pages = _snapshots.Values
                    .SelectMany(x => x.GetDirtyPages(includeCollectionPages));

                foreach (var page in pages.IsLast())
                {
                    // update page transactionID
                    page.Item.TransactionID = this.TransactionID;

                    // if last page, mask as confirm (if requested)
                    if (page.IsLast)
                    {
                        page.Item.IsConfirmed = markLastAsConfirmed;
                    }

                    var buffer = page.Item.GetBuffer(true);

                    // buffer position will be set at end of file (it´s always log file)
                    yield return buffer;

                    _transPages.DirtyPages[page.Item.PageID] = new PagePosition(page.Item.PageID, buffer.Position);
                }
            };

            // write all pages, in sequence on log-file and store references into log pages on transPages
            _disk.LogWriter.Write(source());

            // clear local pages in all snapshots
            foreach (var snapshot in _snapshots.Values)
            {
                snapshot.Clear();
            }

            // there is no local pages in cache and all dirty pages are in log file
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
                    // persist all pages into log file (mark last page as confirmed if has no header change)
                    this.PersistDirtyPages(true, _transPages.HeaderChanged == false);

                    // if header was changed, be last page to persist (and mark as confirm)
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
                                    var empty = _reader.NewPage();

                                    var lastDeletedPage = new BasePage(empty, _transPages.LastDeletedPageID, PageType.Empty)
                                    {
                                        // update nextPageID of last deleted page to old first page ID
                                        NextPageID = _header.FreeEmptyPageID,
                                        TransactionID = this.TransactionID
                                    };

                                    // this page will write twice on wal, but no problem, only this last version will be saved on data file
                                    _disk.LogWriter.Write(new [] { lastDeletedPage.GetBuffer(true) });

                                    // release page just after write on disk
                                    empty.Release();
                                }
                            }

                            // update this confirm page with current transactionID
                            _header.FreeEmptyPageID = newEmptyPageID;
                            _header.TransactionID = this.TransactionID;

                            // this header page will be marked as confirmed page in log file
                            _header.IsConfirmed = true;

                            // invoke all header callbacks (new/drop collections)
                            _transPages.OnCommit(_header);

                            // clone header page
                            var buffer = _reader.NewPage();

                            Buffer.BlockCopy(_header.GetBuffer(true).Array, 0, buffer.Array, buffer.Offset, buffer.Count);

                            // persist header in log file
                            _disk.LogWriter.Write(new[] { buffer });

                            // release page just after write on disk
                            buffer.Release();

                            // and update wal-index (before release _header lock)
                            _walIndex.ConfirmTransaction(this.TransactionID, _transPages.DirtyPages.Values);
                        }
                    }
                    else if (_transPages.DirtyPages.Count > 0)
                    {
                        // update wal-index 
                        _walIndex.ConfirmTransaction(this.TransactionID, _transPages.DirtyPages.Values);
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
                foreach (var snapshot in _snapshots.Values)
                {
                    snapshot.Dispose();
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
        /// Log file all new pages as EmptyPage in a linked order - also, update SharedPage before store
        /// </summary>
        public void ReturnNewPages()
        {
            // create new transaction ID
            var transactionID = System.Diagnostics.Stopwatch.GetTimestamp();

            // now lock header to update FreePageList
            lock (_header)
            {
                // persist all empty pages into wal-file
                var pagePositions = new Dictionary<uint, PagePosition>();

                IEnumerable<PageBuffer> source()
                {
                    // create list of empty pages with forward link pointer
                    for (var i = 0; i < _transPages.NewPages.Count; i++)
                    {
                        var pageID = _transPages.NewPages[i];
                        var next = i < _transPages.NewPages.Count - 1 ? _transPages.NewPages[i + 1] : _header.FreeEmptyPageID;

                        var buffer = _disk.NewPage();

                        var page = new BasePage(buffer, pageID, PageType.Empty)
                        {
                            NextPageID = next,
                            TransactionID = transactionID
                        };

                        yield return page.GetBuffer(true);

                        buffer.Release();

                        // update wal
                        pagePositions[pageID] = new PagePosition(pageID, buffer.Position);

                    }

                    // update header page with my new transaction ID
                    _header.TransactionID = transactionID;
                    _header.FreeEmptyPageID = _transPages.NewPages[0];
                    _header.IsConfirmed = true;

                    // clone header
                    var clone = _reader.NewPage();
                    var header = new HeaderPage(clone);

                    Buffer.BlockCopy(_header.GetBuffer(true).Array, 0, clone.Array, clone.Offset, clone.Count);

                    yield return clone;

                    clone.Release();
                };

                // write all pages (including new header)
                _disk.LogWriter.Write(source());

                // now confirm this transaction to wal
                _walIndex.ConfirmTransaction(transactionID, pagePositions.Values);
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

            // release memory file thread (stream)
            _reader.Dispose();

            // call done
            _done(this.TransactionID);
        }
    }
}