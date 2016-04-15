using System;

namespace LiteDB
{
    internal partial class DbEngine : IDisposable
    {
        /// <summary>
        /// Implements delete based on a query result
        /// </summary>
        public int Delete(string colName, Query query)
        {
            return this.Transaction<int>(colName, false, (col) =>
            {
                if (col == null) return 0;

                var nodes = query.Run(col, _indexer);
                var count = 0;

                foreach (var node in nodes)
                {
                    _log.Write(Logger.COMMAND, "delete document on '{0}' :: _id = {1}", colName, node.Key);

                    // read dataBlock (do not read all extend pages, i will not use)
                    var dataBlock = _data.GetBlock(node.DataBlock);

                    // lets remove all indexes that point to this in dataBlock
                    foreach (var index in col.GetIndexes(true))
                    {
                        _indexer.Delete(index, dataBlock.IndexRef[index.Slot]);
                    }

                    // remove object data
                    _data.Delete(col, node.DataBlock);

                    _cache.CheckPoint();

                    count++;
                }

                return count;
            });
        }
    }
}