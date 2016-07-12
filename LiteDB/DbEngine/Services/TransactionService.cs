using LiteDB.Interfaces;
using LiteDB.Utils;
using System;
using System.Collections.Generic;

namespace LiteDB
{
    /// <summary>
    /// Manage all transaction and garantee concurrency and recovery
    /// </summary>
    internal class TransactionService
    {
        internal Stack<Transaction> activeTransactions = new Stack<Transaction>();

        private IDiskService _disk;
        private PageService _pager;
        private CacheService _cache;
        private int _level = 0;

        internal TransactionService(IDiskService disk, PageService pager, CacheService cache)
        {
            _disk = disk;
            _pager = pager;
            _cache = cache;
            _cache.MarkAsDirtyAction = (page) => _disk.WriteJournal(page.PageID, page.DiskData);
            _cache.DirtyRecicleAction = () => this.Save();
        }

        /// <summary>
        /// Starts a new transaction - lock database to garantee that only one processes is in a transaction
        /// </summary>
        public Transaction Begin(bool readOnly)
        {
            lock (activeTransactions)
            {
                _level++;

                _disk.Open(readOnly);

                var trans = new Transaction(this);

                activeTransactions.Push(trans);

                return trans;
            }
        }

        /// <summary>
        /// Complete transaction commit dirty pages and closing data file
        /// </summary>
        internal void Complete(Transaction trans)
        {
            lock (activeTransactions)
            {
                popTopTransaction(trans);
                if (activeTransactions.Count > 0) return;

                if (_cache.HasDirtyPages)
                {
                    // save dirty pages
                    this.Save();

                    // delete journal file - datafile is consist here
                    _disk.DeleteJournal();
                }

                // clear all pages in cache
                _cache.Clear();

                // close datafile
                _disk.Close();
            }
        }

        /// <summary>
        /// Save all dirty pages to disk - do not touch on lock disk
        /// </summary>
        private void Save()
        {
            // get header and mark as dirty
            var header = _pager.GetPage<HeaderPage>(0, true);

            // increase file changeID (back to 0 when overflow)
            header.ChangeID = header.ChangeID == ushort.MaxValue ? (ushort)0 : (ushort)(header.ChangeID + (ushort)1);

            // set final datafile length (optimize page writes)
            _disk.SetLength(BasePage.GetSizeOfPages(header.LastPageID + 1));

            // write all dirty pages in data file
            foreach (var page in _cache.GetDirtyPages())
            {
                _disk.WritePage(page.PageID, page.WritePage());
            }
        }

        /// <summary>
        /// Stop transaction, discard journal file and close database
        /// </summary>
        public void Abort(Transaction trans)
        {
            lock (activeTransactions)
            {
                // During an abort, and active transaction becomes invalid
                // Mark them as such, and pull them off the stack
                while (activeTransactions.Count > 0)
                {
                    var temp = activeTransactions.Pop();
                    temp.Cancel();
                }

                // clear all pages from memory (return true if has dirty pages on cache)
                if (_cache.Clear())
                {
                    // if has dirty page, has journal file - delete it (is not valid)
                    _disk.DeleteJournal();
                }

                // release datafile
                _disk.Close();
            }
        }

        private void popTopTransaction(Transaction trans)
        {
            var temp = activeTransactions.Peek();
            if (temp != trans)
            {
                throw new ArgumentException("Invalid transaction on top of stack");
            }
            activeTransactions.Pop();
        }
    }

    public enum TransactionState
    {
        Started,
        Completed,
        Canceled,
        Aborted
    }
    public class Transaction : IDisposable
    {
        public TransactionState State { get; private set; }
        private TransactionService _service;
        internal Transaction(TransactionService _service)
        {
            this._service = _service;
            State = TransactionState.Started;
        }

        public void Commit()
        {
            switch (State)
            {
                case TransactionState.Started:
                    _service.Complete(this);
                    State = TransactionState.Completed;
                    break;
                case TransactionState.Completed:
                    break;
                case TransactionState.Aborted:
                    throw new ArgumentException("Transaction already aborted. Cannot be completed");
                case TransactionState.Canceled:
                    throw new TransactionCancelledException();
            }
        }

        public void Rollback()
        {
            switch (State)
            {
                case TransactionState.Started:
                    _service.Abort(this);
                    State = TransactionState.Aborted;
                    break;
                case TransactionState.Aborted:
                    break;
                case TransactionState.Completed:
                    throw new ArgumentException("Transaction already completed, cannot abort");
                case TransactionState.Canceled:
                    throw new TransactionCancelledException();
            }
        }

        internal void Cancel()
        {
            this.State = TransactionState.Canceled;
        }

        public void Dispose()
        {
            if (State == TransactionState.Started)
            {
                // Only complete it if it's still in process
                this.Commit();
            }
        }
    }
}