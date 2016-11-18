using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
   public partial class DbEngine
    {
        /// <summary>
        /// Get stats from a collection
        /// </summary>
        public CollectionInfo Stats(string colName)
        {
            return this.ReadTransaction<CollectionInfo>(colName, (col) =>
            {
                if (col == null) return null;

                int indexPages, indexFree, dataPages, extendPages, dataFree, docSize;

                this.Usage(col, out indexPages, out indexFree, out dataPages, out extendPages, out dataFree, out docSize);

                return new CollectionInfo
                {
                    Name = colName,
                    DocumentsCount = (int)col.DocumentCount,
                    DocumentAverageSize = (int)((float)docSize / col.DocumentCount),
                    Indexes = this.GetIndexes(colName, true).ToList(),
                    TotalPages = indexPages + dataPages + extendPages + 1,
                    TotalAllocated = BasePage.GetSizeOfPages(indexPages + dataPages + extendPages + 1),
                    TotalFree = indexFree + dataFree,
                    Pages = new Dictionary<string, int>()
                    {
                        { "Index", indexPages },
                        { "Data", dataPages },
                        { "Extend", extendPages }
                    },
                    Allocated = new Dictionary<string, long>()
                    {
                        { "Index", BasePage.GetSizeOfPages(indexPages) },
                        { "Data", BasePage.GetSizeOfPages(dataPages + extendPages) }
                    },
                    Free = new Dictionary<string, long>()
                    {
                        { "Index", indexFree },
                        { "Data", dataFree }
                    }
                };
            });
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