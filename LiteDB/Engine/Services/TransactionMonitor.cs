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
    /// This class monitor all open transactions to manage memory usage for each transaction
    /// </summary>
    internal class TransactionMonitor : IDisposable
    {
        private readonly ConcurrentDictionary<uint, TransactionService> _transactions = new ConcurrentDictionary<uint, TransactionService>();
        private LocalDataStoreSlot _slot = Thread.GetNamedDataSlot(Guid.NewGuid().ToString("n"));

        private readonly HeaderPage _header;
        private readonly LockService _locker;
        private readonly DiskService _disk;
        private readonly WalIndexService _walIndex;
        private readonly EngineSettings _settings;

        private int _freePages;
        private readonly int _initialSize;

        // expose open transactions
        public ICollection<TransactionService> Transactions => _transactions.Values;
        public int FreePages => _freePages;
        public int InitialSize => _initialSize;

        public TransactionMonitor(HeaderPage header, LockService locker, DiskService disk, WalIndexService walIndex, EngineSettings settings)
        {
            _header = header;
            _locker = locker;
            _disk = disk;
            _walIndex = walIndex;
            _settings = settings;

            // initialize free pages with all avaiable pages in memory
            _freePages = settings.MaxTransactionSize;

            // initial size 
            _initialSize = settings.MaxTransactionSize / MAX_OPEN_TRANSACTIONS;
        }

        public TransactionService GetTransaction(bool create, out bool isNew)
        {
            var transaction = Thread.GetData(_slot) as TransactionService;

            if (create && transaction == null)
            {
                if (_transactions.Count >= MAX_OPEN_TRANSACTIONS) throw new LiteException(0, "Maximum number of transactions reached");

                isNew = true;

                var initialSize = this.GetInitialSize();

                transaction = new TransactionService(_header, _locker, _disk, _walIndex, initialSize, this, (id) =>
                {
                    lock(_transactions)
                    {
                        Thread.SetData(_slot, null);

                        _transactions.TryRemove(id, out var t);

                        _freePages += t.MaxTransactionSize;
                    }
                });

                // add transaction to execution transaction dict
                _transactions[transaction.TransactionID] = transaction;

                Thread.SetData(_slot, transaction);
            }
            else
            {
                isNew = false;
            }

            return transaction;
        }

        /// <summary>
        /// Get initial transaction size - get from free pages or reducing from all open transactions
        /// </summary>
        private int GetInitialSize()
        {
            lock(_transactions)
            {
                if (_freePages >= _initialSize)
                {
                    _freePages -= _initialSize;

                    return _initialSize;
                }
                else
                {
                    var sum = 0;

                    // if there is no avaiable pages, reduce all open transactions
                    foreach (var trans in _transactions.Values)
                    {
                        var reduce = (trans.MaxTransactionSize / _initialSize);

                        trans.MaxTransactionSize -= reduce;

                        sum += reduce;
                    }

                    return sum;
                }
            }
        }

        /// <summary>
        /// Try extend max transaction size in passed transaction ONLY if contains free pages available
        /// </summary>
        private bool TryExtend(TransactionService trans)
        {
            lock(_transactions)
            {
                if (_freePages >= _initialSize)
                {
                    trans.MaxTransactionSize += _initialSize;

                    _freePages -= _initialSize;

                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Check if transaction size reach limit AND check if is possible extend this limit
        /// </summary>
        public bool CheckSafepoint(TransactionService trans)
        {
            return 
                trans.Pages.TransactionSize >= trans.MaxTransactionSize &&
                this.TryExtend(trans) == false;
        }

        /// <summary>
        /// Abort all open transactions
        /// </summary>
        public void Dispose()
        {
            foreach(var transaction in _transactions.Values)
            {
                transaction.Abort();
            }
        }
    }
}