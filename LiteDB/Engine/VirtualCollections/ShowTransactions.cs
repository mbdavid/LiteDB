using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private IEnumerable<BsonDocument> ShowTransactions()
        {
            foreach (var transaction in _transactions.Values)
            {
                yield return new BsonDocument
                {
                    ["transactionID"] = transaction.TransactionID,
                    ["threadID"] = transaction.ThreadID,
                    ["current"] = transaction.ThreadID == Thread.CurrentThread.ManagedThreadId,
                    ["transactionState"] = transaction.State.ToString(),
                    ["startTime"] = transaction.StartTime
                };
            }
        }
    }
}