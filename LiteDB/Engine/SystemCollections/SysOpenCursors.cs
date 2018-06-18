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
                            ["transactionID"] = transaction.TransactionID,
                            ["threadID"] = transaction.ThreadID,
                            ["collection"] = snapshot.CollectionPage?.CollectionName,
                            ["readVersion"] = snapshot.ReadVersion,
                            ["mode"] = snapshot.Mode.ToString(),
                            ["elapsed"] = cursor.Timer.Elapsed.TotalMilliseconds,
                            ["fetch"] = cursor.FetchCount,
                            ["done"] = cursor.Done
                        };

                    }
                }
            }
        }
    }
}