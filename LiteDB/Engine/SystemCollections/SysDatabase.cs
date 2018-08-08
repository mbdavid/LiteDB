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

            doc["filename"] = _factory.Filename;
            doc["limitSize"] = _wal.WalFile.LimitSize;
            doc["timeout"] = _locker.Timeout.ToString();
            doc["utcDate"] = _utcDate;
            doc["maxMemoryTransactionSize"] = _maxMemoryTransactionSize;

            doc["lastPageID"] = (int)_header.LastPageID;
            doc["freeEmptyPageID"] = (int)_header.FreeEmptyPageID;

            doc["creationTime"] = _header.CreationTime;
            doc["lastCheckpoint"] = _header.LastCheckpoint;

            doc["fileSize"] = _dataFile.Length;
            doc["filePageCount"] = _dataFile.Length / PAGE_SIZE;

            doc["walFileSize"] = _wal.WalFile.Length;
            doc["walFilePageCount"] = _wal.WalFile.Length / PAGE_SIZE;
            doc["walTransactionsCount"] = _wal.ConfirmedTransactions.Count;

            doc["currentReadVersion"] = _wal.CurrentReadVersion;

            doc["userVersion"] = _header.UserVersion;

            yield return doc;
        }
    }
}