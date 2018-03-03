using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace LiteDB
{
    public partial class LiteEngine
    {
        private ConcurrentDictionary<Guid, LiteTransaction> _transactions = new ConcurrentDictionary<Guid, LiteTransaction>();
        private LocalDataStoreSlot _slot = Thread.GetNamedDataSlot(Guid.NewGuid().ToString("n"));

        internal LiteTransaction GetTransaction(out bool isNew)
        {
            var transaction = Thread.GetData(_slot) as LiteTransaction;

            if (transaction == null)
            {
                isNew = true;

                transaction = new LiteTransaction(_header, _locker, _wal, _datafile, _log);

                // add transaction to execution transaction dict
                _transactions[transaction.TransactionID] = transaction;

                // remove from transaction list & thread slot when dispose
                transaction.Done += (o, s) =>
                {
                    var trans = o as LiteTransaction;

                    Thread.SetData(_slot, null);

                    _transactions.TryRemove(trans.TransactionID, out var t);
                };

                Thread.SetData(_slot, transaction);
            }
            else
            {
                isNew = false;
            }

            return transaction;
        }

        /// <summary>
        /// Set an in-use transaction into current thread. Throw InvalidTransactionState exception if not found or not valid state
        /// WARNING: Do not set same transaction in more then 1 thread!
        /// </summary>
        public LiteTransaction SetTransaction(Guid transactionID)
        {
            if (_transactions.TryGetValue(transactionID, out var transaction) == false) throw LiteException.InvalidTransactionState("SetTransaction", TransactionState.Disposed);

            if (transaction.State == TransactionState.New || transaction.State == TransactionState.InUse)
            {
                Thread.SetData(_slot, transaction);

                return transaction;
            }

            throw LiteException.InvalidTransactionState("SetTransaction", transaction.State);
        }

        /// <summary>
        /// Initialize a new transaction
        /// </summary>
        public LiteTransaction BeginTrans()
        {
            var transaction = this.GetTransaction(out var isNew);

            if (isNew == false) throw LiteException.InvalidTransactionState();

            return transaction;
        }

        /// <summary>
        /// Create (or reuse) a transaction an add try/catch block. Commit transaction if is new transaction
        /// </summary>
        private T AutoTransaction<T>(Func<LiteTransaction, T> fn)
        {
            var transaction = this.GetTransaction(out var isNew);

            try
            {
                var result = fn(transaction);

                // if this transaction was auto-created for this operation, commit & dispose now
                if (isNew)
                {
                    transaction.Commit();
                    transaction.Dispose();
                }

                return result;
            }
            catch
            {
                // do rollback and release transaction
                transaction.Dispose();
                throw;
            }
        }
    }
}