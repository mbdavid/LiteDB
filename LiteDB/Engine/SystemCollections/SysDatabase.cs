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
            yield return new BsonDocument { ["_id"] = "extendSegments", ["value"] = _disk.Cache.ExtendSegments };
            yield return new BsonDocument
            {
                ["_id"] = "memoryUsage",
                ["value"] = (_disk.Cache.ExtendSegments * MEMORY_SEGMENT_SIZE * PAGE_SIZE) +
                                                    (40 * (_disk.Cache.ExtendSegments * MEMORY_SEGMENT_SIZE))
            };
            yield return new BsonDocument { ["_id"] = "freePages", ["value"] = _disk.Cache.FreePages };
            yield return new BsonDocument { ["_id"] = "readablePages", ["value"] = _disk.Cache.GetPages().Count };
            yield return new BsonDocument { ["_id"] = "writablePages", ["value"] = _disk.Cache.WritablePages };
            yield return new BsonDocument { ["_id"] = "pagesInUse", ["value"] = _disk.Cache.PagesInUse };
            yield return new BsonDocument { ["_id"] = "openTransactions", ["value"] = _monitor.Transactions.Count };
            yield return new BsonDocument { ["_id"] = "maxOpenTransactions", ["value"] = MAX_OPEN_TRANSACTIONS };
            yield return new BsonDocument { ["_id"] = "initialTransactionSize", ["value"] = _monitor.InitialSize };
            yield return new BsonDocument { ["_id"] = "availableTransactionSize", ["value"] = _monitor.FreePages };
        }
    }
}