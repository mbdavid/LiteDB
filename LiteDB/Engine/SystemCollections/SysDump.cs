using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private IEnumerable<BsonDocument> SysDumpData()
        {
            var length = _dataFile.Length;
            var position = 0;
            var collections = _header.Collections.ToDictionary(x => x.Value, x => x.Key);

            while (position < length)
            {
                var page = _dataFile.ReadPage(position);

                yield return this.DumpPage(page, null, null, false, collections);

                position += PAGE_SIZE;
            }
        }

        private IEnumerable<BsonDocument> SysDumpLog()
        {
            var length = _wal.LogFile.Length;
            var position = 0;
            var versions = new Dictionary<ObjectId, int>();
            var collections = _header.Collections.ToDictionary(x => x.Value, x => x.Key);

            while (position < length)
            {
                var page = _wal.LogFile.ReadPage(position);

                // add versions into an dict grouping al by transactionID
                if (!versions.TryGetValue(page.TransactionID, out var version))
                {
                    if (_wal.Index.TryGetValue(page.PageID, out var slot))
                    {
                        version = slot.Where(x => x.Value == position).Select(x => x.Key).FirstOrDefault();

                        versions.Add(page.TransactionID, version);
                    }
                }

                var doc = this.DumpPage(page, position, version, true, collections);

                yield return doc;

                position += PAGE_SIZE;
            }
        }

        /// <summary>
        /// Dump page information into a BsonDocument
        /// </summary>
        private BsonDocument DumpPage(BasePage page, long? position, int? version, bool transactionID, Dictionary<uint, string> collections)
        {
            var doc = new BsonDocument();

            if (position.HasValue) doc["_position"] = position.Value;
            if (version.HasValue) doc["_version"] = version.Value;

            doc["pageID"] = (int)page.PageID;
            doc["pageType"] = page.PageType.ToString();

            if (transactionID) doc["transactionID"] = page.TransactionID;
            if (transactionID) doc["isConfirmed"] = page.IsConfirmed;

            doc["prevPageID"] = dumpPageID(page.PrevPageID);
            doc["nextPageID"] = dumpPageID(page.NextPageID);
            doc["itemCount"] = (int)page.ItemCount;
            doc["usedBytes"] = (int)(PAGE_SIZE - page.FreeBytes);
            doc["freeBytes"] = (int)page.FreeBytes;

            if (collections.TryGetValue(page.ColID, out var collection))
            {
                doc["collection"] = collection;
            }
            else
            {
                doc["collection"] = dumpPageID(page.ColID);
            }

            if (page.PageType == PageType.Header)
            {
                var header = page as HeaderPage;

                doc["freeEmptyPageID"] = dumpPageID(header.FreeEmptyPageID);
                doc["lastPageID"] = (int)header.LastPageID;
                doc["creationTime"] = header.CreationTime;
                doc["lastCheckpoint"] = header.LastCheckpoint;
                doc["userVersion"] = header.UserVersion;

                doc["colections"] = new BsonArray(header.Collections.Select(x => new BsonDocument
                {
                    ["name"] = x.Key,
                    ["pageID"] = (int)x.Value
                }));
            }
            else if (page.PageType == PageType.Collection)
            {
                var col = page as CollectionPage;

                doc["collectionName"] = col.CollectionName;
                doc["freeDataPageID"] = dumpPageID(col.FreeDataPageID);
                doc["creationTime"] = col.CreationTime;
                doc["lastAnalyzed"] = col.LastAnalyzed;
                doc["indexes"] = new BsonArray(col.GetIndexes(true).Select(x => new BsonDocument
                {
                    ["slot"] = x.Slot,
                    ["name"] = x.Name,
                    ["expression"] = x.Expression,
                    ["unique"] = x.Unique,
                    ["freeIndexPageID"] = dumpPageID(x.FreeIndexPageID),
                    ["headPageID"] = dumpPageID(x.HeadNode.PageID),
                    ["tailPageID"] = dumpPageID(x.TailNode.PageID),
                    ["maxLevel"] = (int)x.MaxLevel,
                    ["keyCount"] = (int)x.KeyCount,
                    ["uniqueKeyCount"] = (int)x.UniqueKeyCount
                }));
            }
            // all other page types contains data-only

            return doc;

            BsonValue dumpPageID(uint pageID)
            {
                return pageID == uint.MaxValue ? BsonValue.Null : new BsonValue((int)pageID);
            }
        }
    }
}