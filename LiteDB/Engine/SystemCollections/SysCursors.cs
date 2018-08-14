using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private IEnumerable<BsonDocument> SysCursors()
        {
            foreach(var cursor in _cursors)
            {
                yield return new BsonDocument
                {
                    ["_id"] = cursor.CursorID,
                    ["transactionID"] = cursor.TransactionID,
                    ["collection"] = cursor.CollectionName,
                    ["readVersion"] = cursor.ReadVersion,
                    ["mode"] = cursor.Mode.ToString(),
                    ["elapsedMS"] = cursor.Timer.Elapsed.TotalMilliseconds,
                    ["elapsed"] = cursor.Timer.Elapsed.ToString(),
                    ["documentLoad"] = cursor.DocumentLoad,
                    ["documentCount"] = cursor.DocumentCount,
                    ["done"] = cursor.Done
                };
            }
        }
    }
}