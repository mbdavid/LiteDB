using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private IEnumerable<BsonDocument> SysDatabase()
        {
            yield return new BsonDocument { ["_id"] = "name", ["value"] = _disk.GetName(FileOrigin.Data) };
            yield return new BsonDocument { ["_id"] = "encrypted", ["value"] = _settings.Password != null };
            yield return new BsonDocument { ["_id"] = "readOnly", ["value"] = _settings.ReadOnly };
            yield return new BsonDocument { ["_id"] = "lastPageID", ["value"] = (int)_header.LastPageID };
            yield return new BsonDocument { ["_id"] = "freeEmptyPageID", ["value"] = (int)_header.FreeEmptyPageList };
            yield return new BsonDocument { ["_id"] = "creationTime", ["value"] = _header.CreationTime };
            yield return new BsonDocument { ["_id"] = "dataFileSize", ["value"] = (int)_disk.GetLength(FileOrigin.Data) };
            yield return new BsonDocument { ["_id"] = "logFileSize", ["value"] = (int)_disk.GetLength(FileOrigin.Log) };
            yield return new BsonDocument { ["_id"] = "asyncQueueLength", ["value"] = _disk.Queue.Length };
            yield return new BsonDocument { ["_id"] = "currentReadVersion", ["value"] = _walIndex.CurrentReadVersion };
            yield return new BsonDocument { ["_id"] = "lastTransactionID", ["value"] = _walIndex.LastTransactionID };

            yield return new BsonDocument
            {
                ["_id"] = "cache",
                ["value"] = new BsonDocument
                {
                    ["extendSegments"] = _disk.Cache.ExtendSegments,
                    ["memoryUsage"] =               (_disk.Cache.ExtendSegments * MEMORY_SEGMENT_SIZE * PAGE_SIZE) +
                                                    (40 * (_disk.Cache.ExtendSegments * MEMORY_SEGMENT_SIZE)),
                    ["freePages"] = _disk.Cache.FreePages,
                    ["readablePages"] = _disk.Cache.GetPages().Count,
                    ["writablePages"] = _disk.Cache.WritablePages,
                    ["pagesInUse"] = _disk.Cache.PagesInUse,
                }
            };

            yield return new BsonDocument
            {
                ["_id"] = "transactions",
                ["value"] = new BsonDocument
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