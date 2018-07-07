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
                var page = _dataFile.ReadPage(position, false);

                yield return this.DumpPage(page, null, null, false, false, collections);

                position += PAGE_SIZE;
            }
        }

        private IEnumerable<BsonDocument> SysDumpWal()
        {
            var length = _wal.WalFile.Length;
            var position = 0;
            var versions = new Dictionary<Guid, int>();
            var collections = _header.Collections.ToDictionary(x => x.Value, x => x.Key);

            while (position < length)
            {
                var page = _wal.WalFile.ReadPage(position, false);

                // add versions into an dict grouping al by transactionID
                if (!versions.TryGetValue(page.TransactionID, out var version))
                {
                    if (_wal.Index.TryGetValue(page.PageID, out var slot))
                    {
                        version = slot.Where(x => x.Value == position).Select(x => x.Key).FirstOrDefault();

                        versions.Add(page.TransactionID, version);
                    }
                }

                var doc = this.DumpPage(page, position, version, true, false, collections);

                yield return doc;

                position += PAGE_SIZE;
            }
        }

        /// <summary>
        /// Dump page information into a BsonDocument
        /// </summary>
        private BsonDocument DumpPage(BasePage page, long? position, int? version, bool transactionID, bool dirty, Dictionary<uint, string> collections)
        {
            var doc = new BsonDocument();

            if (position.HasValue) doc["_position"] = position.Value;
            if (version.HasValue) doc["_version"] = version.Value;
            if (dirty) doc["_dirty"] = page.IsDirty;

            doc["pageID"] = (int)page.PageID;
            doc["pageType"] = page.PageType.ToString();

            if (transactionID) doc["transactionID"] = page.TransactionID;

            doc["prevPageID"] = (int)page.PrevPageID;
            doc["nextPageID"] = (int)page.NextPageID;
            doc["itemCount"] = (int)page.ItemCount;
            doc["freeBytes"] = (int)page.FreeBytes;

            if (collections.TryGetValue(page.ColID, out var collection))
            {
                doc["collection"] = collection;
            }
            else
            {
                doc["collection"] = page.ColID == uint.MaxValue ? BsonValue.Null : new BsonValue((int)page.ColID);
            }

            if (page.PageType == PageType.Header)
            {
                var header = page as HeaderPage;

                doc["freeEmptyPageID"] = (int)header.FreeEmptyPageID;
                doc["lastPageID"] = (int)header.LastPageID;
                doc["creationTime"] = header.CreationTime;
                doc["lastCommit"] = header.LastCommit;
                doc["lastCheckpoint"] = header.LastCheckpoint;
                doc["commitCounter"] = (int)header.CommitCounter;
                doc["userVersion"] = header.UserVersion;

                doc["colections"] = new BsonArray(header.Collections.Select(x => new BsonDocument
                {
                    ["name"] = x.Key,
                    ["pageID"] = (int)x.Value
                }));

            }
            else if (page.PageType == PageType.Collection)
            {
                var colPage = page as CollectionPage;

                doc["collectionName"] = colPage.CollectionName;
                doc["freeDataPageID"] = (int)colPage.FreeDataPageID;
                doc["documentCount"] = (int)colPage.DocumentCount;
                doc["sequence"] = (int)colPage.Sequence;
                doc["creationTime"] = colPage.CreationTime;
                doc["indexes"] = new BsonArray(colPage.GetIndexes(true).Select(x => new BsonDocument
                {
                    ["slot"] = x.Slot,
                    ["name"] = x.Name,
                    ["expression"] = x.Expression,
                    ["unique"] = x.Unique,
                    ["headPageID"] = (int)x.HeadNode.PageID,
                    ["maxLevel"] = (int)x.MaxLevel,
                    ["keyCount"] = (int)x.KeyCount,
                    ["uniqueKeyCount"] = (int)x.UniqueKeyCount
                }));
            }
            // all other page types contains data-only


            return doc;
        }
    }
}