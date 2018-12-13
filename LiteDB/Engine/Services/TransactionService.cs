using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        public readonly int _threadID = Thread.CurrentThread.ManagedThreadId;
        public readonly long _transactionID = System.Diagnostics.Stopwatch.GetTimestamp();
        public TransactionState _state = TransactionState.New;

        // expose
        public long TransactionID => _transactionID;

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
        /// Return if this transaction is readonly or writable. If any snapshot are writable, return as Writable.
        /// </summary>
        public LockMode Mode => _snapshots.Any(x => x.Value.Mode == LockMode.Write) ? LockMode.Write : LockMode.Read;

        /// <summary>
        /// Create (or get from transaction-cache) snapshot and return
        /// </summary>
        public Snapshot CreateSnapshot(LockMode mode, string collection, bool addIfNotExists)
        {
            // if transaction are commited/aborted do not accept new snapshots
            if (_state == TransactionState.Commited || _state == TransactionState.Aborted) throw LiteException.InvalidTransactionState("CreateSnapshot", _state);

            _state = TransactionState.Active;

            Snapshot create() => new Snapshot(mode, collection, _header, _transactionID, _transPages, _locker, _walIndex, _reader, addIfNotExists);

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
            ENSURE(_state == TransactionState.Active, "Safepoint() are called during an invalid transaction state");

            if (_transPages.TransactionSize >= MAX_TRANSACTION_SIZE)
            {
                // if any snapshot are writable, persist pages
                if (this.Mode == LockMode.Write)
                {
                    this.PersistDirtyPages(false, false);
                }

                // clear local pages in all snapshots (read/write snapshosts)
                foreach (var snapshot in _snapshots.Values)
                {
                    snapshot.Clear();
                }

                // there is no local pages in cache and all dirty pages are in log file
                _transPages.TransactionSize = 0;
            }
        }

        /// <summary>
        /// Persist all dirty in-memory pages (in all snapshots) and clear local pages (even clean pages)
        /// </summary>
        private void PersistDirtyPages(bool includeCollectionPages, bool markLastAsConfirmed)
        {
            var dirty = 0;

            // inner method to get all dirty pages
            IEnumerable<PageBuffer> source()
            {
                // get all dirty pages from all write snapshots
                // can include (or not) collection pages
                // update DirtyPagesLog inside transPage for all dirty pages was write on disk
                var pages = _snapshots.Values
                    .Where(x => x.Mode == LockMode.Write)
                    .SelectMany(x => x.GetWritablePages(true, includeCollectionPages));

                foreach (var page in pages.IsLast())
                {
                    // update page transactionID
                    page.Item.TransactionID = _transactionID;

                    // if last page, mask as confirm (if requested)
                    if (page.IsLast)
                    {
                        page.Item.IsConfirmed = markLastAsConfirmed;
                    }

                    var buffer = page.Item.GetBuffer(true);

                    // buffer position will be set at end of file (it´s always log file)
                    yield return buffer;

                    dirty++;

                    _transPages.DirtyPages[page.Item.PageID] = new PagePosition(page.Item.PageID, buffer.Position);
                }
            };

            // write all dirty pages, in sequence on log-file and store references into log pages on transPages
            // (works only for Write snapshots)
            _disk.WriteAsync(source(), FileOrigin.Log);

            LOG($"writing dirty pages ({dirty})", "TRANSACTION");

            // now, discard all clean pages (because those pages are writable and must be readable)
            // from write snapshots
            _disk.DiscardPages(_snapshots.Values
                    .Where(x => x.Mode == LockMode.Write)
                    .SelectMany(x => x.GetWritablePages(false, includeCollectionPages))
                    .Select(x => x.GetBuffer(false)), false);
        }

        /// <summary>
        /// Write pages into disk and confirm transaction in wal-index. Returns true if any dirty page was updated
        /// </summary>
        public bool Commit()
        {
            if (_state == TransactionState.Commited || _state == TransactionState.Aborted) return false;

            if (_state == TransactionState.Active)
            {
                if (this.Mode == LockMode.Write)
                {
                    // persist all pages into log file (mark last page as confirmed if has no header change)
                    // also, release all data/index pages
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
                                        TransactionID = _transactionID
                                    };

                                    // this page will write twice on wal, but no problem, only this last version will be saved on data file
                                    _disk.WriteAsync(new [] { lastDeletedPage.GetBuffer(true) }, FileOrigin.Log);

                                    // release page just after write on disk
                                    empty.Release();
                                }
                            }

                            // create a header save point before any change
                            _header.Savepoint();

                            try
                            {
                                // update this confirm page with current transactionID
                                _header.FreeEmptyPageID = newEmptyPageID;
                                _header.TransactionID = _transactionID;

                                // this header page will be marked as confirmed page in log file
                                _header.IsConfirmed = true;

                                // invoke all header callbacks (new/drop collections)
                                _transPages.OnCommit(_header);

                                // clone header page
                                var buffer = _header.GetBuffer(true);
                                var clone = _reader.NewPage();

                                Buffer.BlockCopy(buffer.Array, buffer.Offset, clone.Array, clone.Offset, clone.Count);

                                // persist header in log file
                                _disk.WriteAsync(new[] { clone }, FileOrigin.Log);

                                // release page just after write on disk
                                clone.Release();
                            }
                            catch
                            {
                                // must revert all header content if any error occurs during header change
                                _header.RestoreSavepoint();
                                throw;
                            }

                            // and update wal-index (before release _header lock)
                            _walIndex.ConfirmTransaction(_transactionID, _transPages.DirtyPages.Values);
                        }
                    }
                    else if (_transPages.DirtyPages.Count > 0)
                    {
                        // update wal-index 
                        _walIndex.ConfirmTransaction(_transactionID, _transPages.DirtyPages.Values);
                    }
                }

                // dispose all snapshosts
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
            if (_state == TransactionState.Commited || _state == TransactionState.Aborted) return false;

            if (_state == TransactionState.Active)
            {
                // if transaction contains new pages, must return to database in another transaction
                if (_transPages.NewPages.Count > 0)
                {
                    this.ReturnNewPages();
                }

                // dispose all snaphosts
                foreach (var snapshot in _snapshots.Values)
                {
                    // but first, if writable, discard changes
                    if (snapshot.Mode == LockMode.Write)
                    {
                        // discard all dirty pages
                        _disk.DiscardPages(snapshot.GetWritablePages(true, true).Select(x => x.GetBuffer(false)), true);

                        // discard all clean pages
                        _disk.DiscardPages(snapshot.GetWritablePages(false, true).Select(x => x.GetBuffer(false)), false);
                    }

                    // now, release pages
                    snapshot.Dispose();
                }
            }

            this.Done(TransactionState.Aborted);

            return true;
        }

        /// <summary>
        /// Return added pages when occurs an rollback transaction (run this only in rollback). Create new transactionID and add into
        /// Log file all new pages as EmptyPage in a linked order - also, update SharedPage before store
        /// </summary>
        private void ReturnNewPages()
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

                    // clone header buffer
                    var buf = _header.GetBuffer(true);
                    var clone = _reader.NewPage();

                    Buffer.BlockCopy(buf.Array, buf.Offset, clone.Array, clone.Offset, clone.Count);

                    yield return clone;

                    clone.Release();
                };

                // create a header save point before any change
                _header.Savepoint();

                try
                {
                    // write all pages (including new header)
                    _disk.WriteAsync(source(), FileOrigin.Log);
                }
                catch
                {
                    // must revert all header content if any error occurs during header change
                    _header.RestoreSavepoint();
                    throw;
                }

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
            if (Thread.CurrentThread.ManagedThreadId == _threadID)
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
            _state = state;

            // release thread transaction lock
            _locker.ExitTransaction();

            // release memory file thread (stream)
            _reader.Dispose();

            // call done
            _done(_transactionID);
        }
    }
}