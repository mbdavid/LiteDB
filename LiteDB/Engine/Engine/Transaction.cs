using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private ConcurrentDictionary<Guid, TransactionService> _transactions = new ConcurrentDictionary<Guid, TransactionService>();
        private LocalDataStoreSlot _slot = Thread.GetNamedDataSlot(Guid.NewGuid().ToString("n"));

        internal TransactionService GetTransaction(bool create, out bool isNew)
        {
            // if engine are disposing, do not accept any transaction/operation
            if (_shutdown) throw LiteException.DatabaseShutdown();

            var transaction = Thread.GetData(_slot) as TransactionService;

            if (create && transaction == null)
            {
                isNew = true;

                transaction = new TransactionService(_header, _locker, _dataFile, _wal, _maxMemoryTransactionSize, _log, (id) =>
                {
                    Thread.SetData(_slot, null);

                    _transactions.TryRemove(id, out var t);
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
        /// Initialize a new transaction. Transaction are created "per-thread". There is only one single transaction per thread.
        /// Return true if transaction was created or false if current thread already in a transaction.
        /// </summary>
        public bool BeginTrans()
        {
            var transacion = this.GetTransaction(true, out var isNew);

            return isNew;
        }

        /// <summary>
        /// Persist all dirty pages into WAL file using async task. 
        /// </summary>
        public bool Commit()
        {
            var transaction = this.GetTransaction(false, out var isNew);

            if (transaction != null)
            {
                return transaction.Commit();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Do rollback to current transaction. Clear dirty pages in memory and return new pages to main empty linked-list
        /// </summary>
        public bool Rollback()
        {
            var transaction = this.GetTransaction(false, out var isNew);

            if (transaction != null)
            {
                return transaction.Rollback();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Create (or reuse) a transaction an add try/catch block. Commit transaction if is new transaction
        /// </summary>
        private T AutoTransaction<T>(Func<TransactionService, T> fn)
        {
            var transaction = this.GetTransaction(true, out var isNew);

            try
            {
                var result = fn(transaction);

                // if this transaction was auto-created for this operation, commit & dispose now
                if (isNew)
                {
                    transaction.Commit();
                }

                return result;
            }
            catch(Exception ex)
            {
                _log.Error(ex);

                transaction.Rollback();

                throw;
            }
        }
    }
}