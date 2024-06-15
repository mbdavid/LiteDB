namespace LiteDB.Engine;

using System;
using System.Collections.Generic;

public partial class LiteEngine
{
    private IEnumerable<BsonDocument> SysOpenCursors()
    {
        foreach (var transaction in _monitor.Transactions)
        {
            foreach (var cursor in transaction.OpenCursors)
            {
                yield return new BsonDocument
                {
                    ["threadID"] = transaction.ThreadID,
                    ["transactionID"] = (int) transaction.TransactionID,
                    ["elapsedMS"] = (int) cursor.Elapsed.ElapsedMilliseconds,
                    ["collection"] = cursor.Collection,
                    ["mode"] = cursor.Query.ForUpdate ? "write" : "read",
                    ["sql"] = cursor.Query.ToSQL(cursor.Collection).Replace(Environment.NewLine, " "),
                    ["running"] = cursor.Elapsed.IsRunning,
                    ["fetched"] = cursor.Fetched
                };
            }
        }
    }
}