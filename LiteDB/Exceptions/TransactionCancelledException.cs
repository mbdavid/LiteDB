using System;

namespace LiteDB
{
    public class TransactionCancelledException : LiteException
    {
        public TransactionCancelledException()
            : base("This transaction is cancelled due to an aborted transaction")
        {

        }
    }
}
