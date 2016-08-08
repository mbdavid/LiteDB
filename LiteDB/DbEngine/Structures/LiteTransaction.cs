using System;
using System.Collections.Generic;

namespace LiteDB
{
    public enum TransactionState
    {
        Started,
        Completed,
        Canceled,
        Aborted
    }

    public class LiteTransaction : IDisposable
    {
        public TransactionState State { get; private set; }

        private TransactionService _service;

        internal LiteTransaction(TransactionService service)
        {
            this.State = TransactionState.Started;

            _service = service;
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
                    _service.Abort();
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
                // only complete it if it's still in process
                this.Commit();
            }
        }
    }
}