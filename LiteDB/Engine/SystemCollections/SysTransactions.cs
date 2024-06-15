namespace LiteDB.Engine;

using System.Collections.Generic;
using System.Linq;

public partial class LiteEngine
{
    private IEnumerable<BsonDocument> SysTransactions()
    {
        foreach (var transaction in _monitor.Transactions)
        {
            yield return new BsonDocument
            {
                ["threadID"] = transaction.ThreadID,
                ["transactionID"] = (int) transaction.TransactionID,
                ["startTime"] = transaction.StartTime,
                ["mode"] = transaction.Mode.ToString(),
                ["transactionSize"] = transaction.Pages.TransactionSize,
                ["maxTransactionSize"] = transaction.MaxTransactionSize,
                ["pagesInLogFile"] = transaction.Pages.DirtyPages.Count,
                ["newPages"] = transaction.Pages.NewPages.Count,
                ["deletedPages"] = transaction.Pages.DeletedPages,
                ["modifiedPages"] = transaction.Snapshots.Select(x => x.GetWritablePages(true, true).Count()).Sum()
            };
        }
    }
}