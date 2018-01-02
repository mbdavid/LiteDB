using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteEngine
    {
        public IEnumerable<BsonDocument> DumpDatabase()
        {
            using (var trans = this.ReadTransaction(null))
            {
                var header = trans.Pager.GetPage<HeaderPage>(0);

                yield return this.DumpPage(header)
                    .Extend(new BsonDocument { ["version"] = trans.Pager.ReadVersion });

                for (uint i = 1; i <= header.LastPageID; i++)
                {
                    var page = trans.Pager.GetPage<BasePage>(i);

                    yield return this.DumpPage(page)
                        .Extend(new BsonDocument { ["version"] = trans.Pager.ReadVersion });
                }
            }
        }

        public IEnumerable<BsonDocument> DumpDataFile()
        {
            return this.DumpFile(_dataFile);
        }

        public IEnumerable<BsonDocument> DumpWalFile()
        {
            return this.DumpFile(_walFile);
        }

        private IEnumerable<BsonDocument> DumpFile(FileService file)
        {
            var length = file.FileSize();
            var position = 0;

            while (position < length)
            {
                var page = file.ReadPage(position);

                yield return this.DumpPage(page);

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

                doc["userVersion"] = (int)header.UserVersion;
                doc["creationTime"] = header.CreationTime;
                doc["freeEmptyPageID"] = (int)header.FreeEmptyPageID;
                doc["lastPageID"] = (int)header.LastPageID;
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
                    ["maxLevel"] = (int)x.MaxLevel,
                    ["headPageID"] = (int)x.HeadNode.PageID
                }));
            }
            // all other page types contains data-only


            return doc;
        }
    }
}