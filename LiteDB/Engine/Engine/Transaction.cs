using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Initialize a new transaction. Transaction are created "per-thread". There is only one single transaction per thread.
        /// Return true if transaction was created or false if current thread already in a transaction.
        /// </summary>
        public bool BeginTrans()
        {
            var transacion = _monitor.GetTransaction(true, false, out var isNew);

            transacion.ExplicitTransaction = true;

            if (transacion.OpenCursors.Count > 0) throw new LiteException(0, "This thread contains an open cursors/query. Close cursors before Begin()");

            LOG(isNew, $"begin trans", "COMMAND");

            return isNew;
        }

        /// <summary>
        /// Persist all dirty pages into LOG file
        /// </summary>
        public bool Commit()
        {
            var transaction = _monitor.GetTransaction(false, false, out _);

            if (transaction != null)
            {
                // do not accept explicit commit transaction when contains open cursors running
                if (transaction.OpenCursors.Count > 0) throw new LiteException(0, "Current transaction contains open cursors. Close cursors before run Commit()");

                if (transaction.State == TransactionState.Active)
                {
                    this.CommitAndReleaseTransaction(transaction);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Do rollback to current transaction. Clear dirty pages in memory and return new pages to main empty linked-list
        /// </summary>
        public bool Rollback()
        {
            var transaction = _monitor.GetTransaction(false, false, out _);

            if (transaction != null && transaction.State == TransactionState.Active)
            {
                transaction.Rollback();

                _monitor.ReleaseTransaction(transaction);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Create (or reuse) a transaction an add try/catch block. Commit transaction if is new transaction
        /// </summary>
        private T AutoTransaction<T>(Func<TransactionService, T> fn)
        {
            var transaction = _monitor.GetTransaction(true, false, out var isNew);

            try
            {
                var result = fn(transaction);

                // if this transaction was auto-created for this operation, commit & dispose now
                if (isNew)
                    this.CommitAndReleaseTransaction(transaction);

                return result;
            }
            catch(Exception ex)
            {
                LOG(ex.Message, "ERROR");

                transaction.Rollback();

                _monitor.ReleaseTransaction(transaction);

                throw;
            }
        }

        private void CommitAndReleaseTransaction(TransactionService transaction)
        {
            transaction.Commit();
            _monitor.ReleaseTransaction(transaction);
            if (_header.Pragmas.Checkpoint > 0 && _disk.GetLength(FileOrigin.Log) > (_header.Pragmas.Checkpoint * PAGE_SIZE))
                _walIndex.TryCheckpoint();
        }
    }
}