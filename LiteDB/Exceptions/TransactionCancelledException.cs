using System;

namespace LiteDB
{
    public class TransactionCancelledException : LiteException
    {
        public TransactionCancelledException()
            : base(TRANSACTION_CANCELLED_EXCEPTION, "This transaction is cancelled due to an aborted transaction")
        {
        }
    }
}
