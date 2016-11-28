using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
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

                    // get all indexes nodes from this data block
                    var allNodes = _indexer.GetNodeList(node, true).ToArray();

                    // lets remove all indexes that point to this in dataBlock
                    foreach (var linkNode in allNodes)
                    {
                        var index = col.Indexes[linkNode.Slot];

                        _indexer.Delete(index, linkNode.Position);
                    }

                    // remove object data
                    _data.Delete(col, node.DataBlock);

                    _trans.CheckPoint();

                    count++;
                }

                return count;
            });
        }
    }
}