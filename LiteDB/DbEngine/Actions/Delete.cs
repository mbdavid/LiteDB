using LiteDB.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal partial class DbEngine : IDisposable
    {
        /// <summary>
        /// Implements delete based on a query result
        /// </summary>
        public int DeleteDocuments(string colName, Query query)
        {
            return this.Transaction<int>(colName, false, (col) =>
            {
                // no collection, no document - abort trans
                if (col == null) return 0;

                var count = 0;

                // find nodes
                var nodes = query.Run(col, _indexer);

                foreach (var node in nodes)
                {
                    // read dataBlock (do not read all extend pages, i will not use)
                    var dataBlock = _data.Read(node.DataBlock, false);

                    // lets remove all indexes that point to this in dataBlock
                    foreach (var index in col.GetIndexes(true))
                    {
                        _indexer.Delete(index, dataBlock.IndexRef[index.Slot]);
                    }

                    // remove object data
                    _data.Delete(col, node.DataBlock);

                    count++;
                }

                return count;
            });
        }
    }
}
