using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private IEnumerable<BsonDocument> SysDatabase()
        {
            var version = typeof(LiteEngine).Assembly.GetName().Version;

            BsonValue number(long num)
            {
                return num < int.MaxValue ? new BsonValue((int)num) : new BsonValue(num);
            }

            yield return new BsonDocument
            {
                ["name"] = _disk.Factory.Name,
                ["encrypted"] = _settings.Password != null,
                ["readOnly"] = _settings.ReadOnly,

                ["lastPageID"] = (int)_header.LastPageID,
                ["freeEmptyPageID"] = (int)_header.FreeEmptyPageList,

                ["creationTime"] = _header.CreationTime,

                ["currentReadVersion"] = _walIndex.CurrentReadVersion,
                ["lastTransactionID"] = _walIndex.LastTransactionID,
                ["engine"] = $"litedb-ce-v{version.Major}.{version.Minor}.{version.Build}",

                ["disk"] = new BsonDocument
                {
                    ["fileSize"] = number(_disk.Factory.GetLength()),
                    ["dataSize"] = number((_header.LastPageID + 1) * PAGE_SIZE),
                    ["asyncQueueLength"] = _disk.Queue.Length,
                },

                ["log"] = new BsonDocument
                {
                    ["size"] = number(_disk.LogLength),
                    ["startPosition"] = number(_disk.LogStartPosition),
                    ["endPosition"] = number(_disk.LogEndPosition)
                },

                ["pragmas"] = new BsonDocument(_header.Pragmas.Pragmas.ToDictionary(x => x.Name, x => x.Get())),

                ["cache"] = new BsonDocument
                {
                    ["extendSegments"] = _disk.Cache.ExtendSegments,
                    ["extendPages"] = _disk.Cache.ExtendPages,
                    ["freePages"] = _disk.Cache.FreePages,
                    ["readablePages"] = _disk.Cache.GetPages().Count,
                    ["writablePages"] = _disk.Cache.WritablePages,
                    ["pagesInUse"] = _disk.Cache.PagesInUse,
                },

                ["transactions"] = new BsonDocument
                {
                    ["open"] = _monitor.Transactions.Count,
                    ["maxOpenTransactions"] = MAX_OPEN_TRANSACTIONS,
                    ["initialTransactionSize"] = _monitor.InitialSize,
                    ["availableSize"] = _monitor.FreePages
                }
            };
        }
    }
}