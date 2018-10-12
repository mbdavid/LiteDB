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
        private IEnumerable<BsonDocument> SysVersions()
        {
            var version = 0;

            foreach (var transaction in _wal.ConfirmedTransactions)
            {
                version++;

                // get page count from all WAL index
                var pages = _wal.Index.SelectMany(x => x.Value.Where(z => z.Key == version)).Count();

                yield return new BsonDocument
                {
                    ["version"] = version,
                    ["transaction"] = transaction,
                    ["startTime"] = transaction.CreationTime,
                    ["pages"] = pages
                };
            }
        }
    }
}