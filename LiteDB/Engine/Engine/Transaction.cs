using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private ConcurrentDictionary<Guid, LiteTransaction> _transactions = new ConcurrentDictionary<Guid, LiteTransaction>();
        private LocalDataStoreSlot _slot = Thread.GetNamedDataSlot(Guid.NewGuid().ToString("n"));

        internal LiteTransaction GetTransaction(out bool isNew)
        {
            // if engine are disposing, do not accept any transaction/operation
            if (_disposing) throw LiteException.DatabaseShutdown();

            var transaction = Thread.GetData(_slot) as LiteTransaction;

            if (transaction == null)
            {
                isNew = true;

                transaction = new LiteTransaction(_header, _locker, _dataFile, _wal, _log);

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