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

            doc["filename"] = _settings.Filename;
            doc["limitSize"] = _settings.LimitSize;
            doc["initialSize"] = _settings.InitialSize;
            doc["maxMemoryTransactionSize"] = _settings.MaxMemoryTransactionSize;
            doc["readOnly"] = _settings.ReadOnly;
            doc["timeout"] = _settings.Timeout.ToString();
            doc["utcDate"] = _settings.UtcDate;

            doc["creationTime"] = _header.CreationTime;
            doc["lastCommit"] = _header.LastCommit;
            doc["lastShrink"] = _header.LastShrink;
            doc["lastCheckpoint"] = _header.LastCheckpoint;
            doc["lastVaccum"] = _header.LastVaccum;

            doc["commitCounter"] = (int)_header.CommitCount;
            doc["checkpointCounter"] = (int)_header.CheckpointCounter;

            doc["fileSize"] = _dataFile.Length;
            doc["filePageCount"] = _dataFile.Length / PAGE_SIZE;
            doc["walSize"] = _wal.WalFile.Length;
            doc["walPageCount"] = _wal.WalFile.Length / PAGE_SIZE;
            doc["walTransactions"] = _wal.ConfirmedTransactions.Count;
            doc["walCurrentReadVersion"] = _wal.CurrentReadVersion;

            doc["userVersion"] = _header.UserVersion;

            yield return doc;
        }
    }
}