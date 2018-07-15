using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private IEnumerable<BsonDocument> SysSnapshots()
        {
            foreach (var transaction in _transactions.Values)
            {
                foreach (var snapshot in transaction.Snapshots.Values)
                {
                    yield return new BsonDocument
                    {
                        ["threadID"] = transaction.ThreadID,
                        ["transactionID"] = transaction.TransactionID,
                        ["transactionState"] = transaction.State.ToString(),
                        ["startTime"] = transaction.StartTime,
                        ["collection"] = snapshot.CollectionPage?.CollectionName,
                        ["mode"] = snapshot.Mode.ToString(),
                        ["readVersion"] = snapshot.ReadVersion,
                        ["localPages"] = snapshot.LocalPagesCount,
                    };
                }
            }
        }
    }
}