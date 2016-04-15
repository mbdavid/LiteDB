using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal partial class DbEngine : IDisposable
    {
        /// <summary>
        /// Get stats from a collection
        /// </summary>
        public BsonValue Stats(string colName)
        {
            var col = this.GetCollectionPage(colName, false);

            if (col == null) return BsonValue.Null;

            int indexPages, indexFree, dataPages, extendPages, dataFree, docSize;

            lock (_locker)
            {
                this.Usage(col, out indexPages, out indexFree, out dataPages, out extendPages, out dataFree, out docSize);
            }

            return new BsonDocument()
                .Add("name", colName)
                .Add("documents", (int)col.DocumentCount)
                .Add("documentAverageSize", (int)((float)docSize / col.DocumentCount))
                .Add("indexes", new BsonArray(this.GetIndexes(colName, true)))
                .Add("pages", new BsonDocument()
                    .Add("index", indexPages)
                    .Add("data", dataPages)
                    .Add("extend", extendPages)
                    .Add("total", indexPages + dataPages + extendPages + 1)
                )
                .Add("usage", new BsonDocument()
                    .Add("allocated", new BsonDocument()
                        .Add("index", BasePage.GetSizeOfPages(indexPages))
                        .Add("data", BasePage.GetSizeOfPages(dataPages + extendPages))
                        .Add("total", BasePage.GetSizeOfPages(indexPages + dataPages + extendPages + 1))
                    )
                    .Add("free", new BsonDocument()
                        .Add("index", indexFree)
                        .Add("data", dataFree)
                        .Add("total", indexFree + dataFree)
                    )
                );
        }

        private void Usage(CollectionPage col,
            out int indexPages,
            out int indexFree,
            out int dataPages,
            out int extendPages,
            out int dataFree,
            out int docSize)
        {
            var pages = new HashSet<uint>();
            indexPages = indexFree = dataPages = extendPages = dataFree = docSize = 0;

            // get all pages from PK index + data/extend pages
            foreach (var node in _indexer.FindAll(col.PK, Query.Ascending))
            {
                if (pages.Contains(node.Position.PageID)) continue;

                pages.Add(node.Position.PageID);
                indexPages++;
                indexFree += node.Page.FreeBytes;

                foreach (var n in node.Page.Nodes.Values.Where(x => !x.DataBlock.IsEmpty))
                {
                    var dataPage = _pager.GetPage<DataPage>(n.DataBlock.PageID, false);

                    if (pages.Contains(dataPage.PageID)) continue;

                    foreach (var block in dataPage.DataBlocks.Values)
                    {
                        var doc = BsonSerializer.Deserialize(_data.Read(block.Position));
                        docSize += doc.GetBytesCount(true);
                    }

                    pages.Add(dataPage.PageID);
                    dataPages++;
                    dataFree += dataPage.FreeBytes;

                    // getting extended pages
                    foreach (var ex in dataPage.DataBlocks.Values.Where(x => x.ExtendPageID != uint.MaxValue))
                    {
                        foreach (var extendPage in _pager.GetSeqPages<ExtendPage>(ex.ExtendPageID))
                        {
                            extendPages++;
                            dataFree += extendPage.FreeBytes;
                        }
                    }
                }

                _cache.CheckPoint();
            }

            // add all others indexes
            foreach (var index in col.GetIndexes(false))
            {
                foreach (var node in _indexer.FindAll(index, Query.Ascending))
                {
                    if (pages.Contains(node.Position.PageID)) continue;

                    pages.Add(node.Position.PageID);
                    indexPages++;
                    indexFree += node.Page.FreeBytes;

                    _cache.CheckPoint();
                }
            }
        }
    }
}