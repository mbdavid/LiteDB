using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB.Utils
{
    public class TransactionCancelledException : LiteException
    {
        public TransactionCancelledException() : base("This transaction is cancelled due to an aborted transaction")
        {

        }
    }
}
