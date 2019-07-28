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
            var doc = new BsonDocument();

            doc["name"] = _disk.GetName(FileOrigin.Data);    
            doc["limitSize"] = (int)_settings.LimitSize;
            doc["timeout"] = _settings.Timeout.TotalSeconds;
            doc["utcDate"] = _settings.UtcDate;
            doc["readOnly"] = _settings.ReadOnly;

            doc["lastPageID"] = (int)_header.LastPageID;
            doc["freeEmptyPageID"] = (int)_header.FreeEmptyPageID;

            doc["creationTime"] = _header.CreationTime;

            doc["dataFileSize"] = (int)_disk.GetLength(FileOrigin.Data);
            doc["logFileSize"] = (int)_disk.GetLength(FileOrigin.Log);
            doc["asyncQueueLength"] = _disk.Queue.Length;

            doc["currentReadVersion"] = _walIndex.CurrentReadVersion;
            doc["lastTransactionID"] = _walIndex.LastTransactionID;

            doc["userVersion"] = _header.UserVersion;

            doc["cache"] = new BsonDocument
            {
                ["extendSegments"] = _disk.Cache.ExtendSegments,
                ["memoryUsage"] = 
                    (_disk.Cache.ExtendSegments * _settings.MemorySegmentSize * PAGE_SIZE) +
                    (40 * (_disk.Cache.ExtendSegments * _settings.MemorySegmentSize)),
                ["freePages"] = _disk.Cache.FreePages,
                ["readablePages"] = _disk.Cache.GetPages().Count,
                ["writablePages"] = _disk.Cache.WritablePages,
                ["pagesInUse"] = _disk.Cache.PagesInUse,
            };

            doc["transactions"] = new BsonDocument
            {
                ["open"] = _monitor.Transactions.Count,
                ["maxOpenTransactions"] = MAX_OPEN_TRANSACTIONS,
                ["initialTransactionSize"] = _monitor.InitialSize,
                ["availableSize"] = _monitor.FreePages,
                ["maxTransactionSize"] = _settings.MaxTransactionSize
            };

            yield return doc;
        }
    }
}