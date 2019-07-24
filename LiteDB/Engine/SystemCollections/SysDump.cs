using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private IEnumerable<BsonDocument> SysDump(FileOrigin origin)
        {
            var collections = _header.GetCollections().ToDictionary(x => x.Value, x => x.Key);

            foreach (var buffer in _disk.ReadFull(origin))
            {
                var page = new BasePage(buffer);
                var pageID = page.PageID;

                if (origin == FileOrigin.Data && buffer.Position > 0 && pageID == 0)
                {
                    // this will fix print PageID in data file bubbles pages 
                    pageID = (uint)(buffer.Position / PAGE_SIZE);
                }

                var doc = new BsonDocument();

                doc["_position"] = (int)buffer.Position;

                doc["pageID"] = (int)pageID;
                doc["pageType"] = page.PageType.ToString();
                doc["nextPageID"] = dumpPageID(page.NextPageID);
                doc["prevPageID"] = dumpPageID(page.PrevPageID);

                doc["collection"] = collections.GetOrDefault(page.ColID, "-");
                doc["transactionID"] = (int)page.TransactionID;
                doc["isConfirmed"] = page.IsConfirmed;

                doc["itemsCount"] = (int)page.ItemsCount;
                doc["freeBytes"] = page.FreeBytes;
                doc["usedBytes"] = (int)page.UsedBytes;
                doc["fragmentedBytes"] = (int)page.FragmentedBytes;
                doc["nextFreePosition"] = (int)page.NextFreePosition;
                doc["highestIndex"] = (int)page.HighestIndex;

                if (page.PageType == PageType.Header)
                {
                    var header = new HeaderPage(buffer);

                    doc["freeEmptyPageID"] = dumpPageID(header.FreeEmptyPageID);
                    doc["lastPageID"] = (int)header.LastPageID;
                    doc["creationTime"] = header.CreationTime;
                    doc["userVersion"] = header.UserVersion;
                    doc["collections"] = new BsonDocument(header.GetCollections().ToDictionary(x => x.Key, x => new BsonValue((int)x.Value)));
                }
                else if(page.PageType == PageType.Collection)
                {
                    var collection = new CollectionPage(buffer);

                    doc["lastAnalyzed"] = collection.LastAnalyzed;
                    doc["creationTime"] = collection.CreationTime;
                    doc["freeDataPageID"] = new BsonArray(collection.FreeDataPageID.Select(x => dumpPageID(x)));
                    doc["freeIndexPageID"] = new BsonArray(collection.FreeIndexPageID.Select(x => dumpPageID(x)));
                    doc["indexes"] = new BsonArray(collection.GetCollectionIndexes().Select(x => new BsonDocument
                    {
                        ["name"] = x.Name,
                        ["expression"] = x.Expression,
                        ["unique"] = x.Unique,
                        ["headPageID"] = dumpPageID(x.Head.PageID),
                        ["tailPageID"] = dumpPageID(x.Tail.PageID),
                        ["maxLevel"] = (int)x.MaxLevel,
                        ["keyCount"] = (int)x.KeyCount,
                        ["uniqueKeyCount"] = (int)x.UniqueKeyCount
                    }));
                }

                yield return doc;
            }

            BsonValue dumpPageID(uint pageID)
            {
                return pageID == uint.MaxValue ? BsonValue.Null : new BsonValue((int)pageID);
            }
        }
    }
}