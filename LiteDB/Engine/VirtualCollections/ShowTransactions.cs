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
                var write = transaction.Snapshots.Values.Where(x => x.Mode == SnapshotMode.Write).Any();

                yield return new BsonDocument
                {
                    ["transactionID"] = transaction.TransactionID,
                    ["threadID"] = transaction.ThreadID,
                    ["current"] = transaction.ThreadID == Thread.CurrentThread.ManagedThreadId,
                    ["transactionState"] = transaction.State.ToString(),
                    ["startTime"] = transaction.StartTime,
                    ["mode"] = write ? "Write" : "Read",
                    ["memoryTransactionSize"] = transaction.Pages.TransactionSize,
                    ["walIndexSize"] = transaction.Pages.DirtyPagesWal.Count,
                    ["newPages"] = transaction.Pages.NewPages.Count,
                    ["deletedPages"] = transaction.Pages.DeletedPages,
                    ["newCollections"] = transaction.Pages.NewCollections.Count,
                    ["deletedCollections"] = transaction.Pages.DeletedCollections.Count,
                };
            }
        }
    }
}