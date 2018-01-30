using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteEngine
    {
        public IEnumerable<BsonDocument> DumpDatafile()
        {
            var length = _datafile.Length;
            var position = 0;

            while (position < length)
            {
                // skip page 3 (lock page)
                if (position != BasePage.GetPagePosition(2))
                {
                    var page = _datafile.ReadPage(position);

                    yield return this.DumpPage(page);
                }
                else
                {
                    // lock page - just print empty page
                    yield return this.DumpPage(new EmptyPage(2));
                }

                position += BasePage.PAGE_SIZE;
            }
        }

        /// <summary>
        /// Dump page information into a BsonDocument
        /// </summary>
        private BsonDocument DumpPage(BasePage page)
        {
            var doc = new BsonDocument
            {
                ["pageID"] = (int)page.PageID,
                ["pageType"] = page.PageType.ToString(),
                ["prevPageID"] = (int)page.PrevPageID,
                ["nextPageID"] = (int)page.NextPageID,
                ["itemCount"] = (int)page.ItemCount,
                ["freeBytes"] = (int)page.FreeBytes,
                ["transactionID"] = page.TransactionID.ToString()
            };

            if (page.PageType == PageType.Header)
            {
                var header = page as HeaderPage;

                doc["freeEmptyPageID"] = (int)header.FreeEmptyPageID;
                doc["lastPageID"] = (int)header.LastPageID;
                doc["creationTime"] = header.CreationTime;
                doc["lastCommit"] = header.LastCommit;
                doc["lastCheckpoint"] = header.LastCheckpoint;
                doc["lastAnalyze"] = header.LastAnalyze;
                doc["lastVaccum"] = header.LastVaccum;
                doc["lastShrink"] = header.LastShrink;
                doc["commitCounter"] = (int)header.CommitCount;
                doc["checkpointCounter"] = (int)header.CheckpointCounter;
            }
            else if (page.PageType == PageType.CollectionList)
            {
                var colList = page as CollectionListPage;

                doc["colections"] = new BsonArray(colList.GetAll().Select(x => new BsonDocument
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