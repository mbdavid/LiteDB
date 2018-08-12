using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private IEnumerable<BsonDocument> SysOpenCursors()
        {
            foreach (var transaction in _transactions.Values)
            {
                foreach (var snapshot in transaction.Snapshots.Values)
                {
                    foreach(var cursor in snapshot.Cursors)
                    {
                        yield return new BsonDocument
                        {
                            ["threadID"] = transaction.ThreadID,
                            ["transactionID"] = transaction.TransactionID,
                            ["collection"] = snapshot.CollectionPage?.CollectionName,
                            ["readVersion"] = snapshot.ReadVersion,
                            ["mode"] = snapshot.Mode.ToString(),
                            ["elapsedMS"] = cursor.Timer.Elapsed.TotalMilliseconds,
                            ["elapsed"] = cursor.Timer.Elapsed.ToString(),
                            ["documentFetch"] = cursor.DocumentFetch,
                            ["documentResult"] = cursor.DocumentResult,
                            ["done"] = cursor.Done
                        };
                    }
                }
            }
        }
    }
}