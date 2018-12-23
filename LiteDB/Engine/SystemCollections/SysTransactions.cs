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
        private IEnumerable<BsonDocument> SysTransactions()
        {
            foreach (var transaction in _transactions.Values)
            {
                yield return new BsonDocument
                {
                    ["threadID"] = transaction.ThreadID,
                    ["transactionID"] = (int)transaction.TransactionID,
                    ["transactionState"] = transaction.State.ToString(),
                    //["startTime"] = transaction,
                    ["mode"] = transaction.Mode.ToString(),
                    //["snapshots"] = transaction.Snapshots.Count(),
                    //["pagesInMemory"] = transaction.Snapshots.Values.Select(x => x.LocalPagesCount).Sum(),
                    //["pagesInLogFile"] = transaction.Pages.DirtyPagesWal.Count,
                    //["newPages"] = transaction.Pages.NewPages.Count,
                    //["deletedPages"] = transaction.Pages.DeletedPages,
                    //["newCollections"] = transaction.Pages.NewCollections.Count
                };
            }
        }
    }
}