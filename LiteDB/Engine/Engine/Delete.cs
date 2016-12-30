using System;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Implement delete command based on _id value. Returns true if deleted
        /// </summary>
        public bool Delete(string collection, BsonValue id)
        {
            return this.Delete(collection, Query.EQ("_id", id)) == 1;
        }

        /// <summary>
        /// Implements delete based on a query result
        /// </summary>
        public int Delete(string collection, Query query)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException("collection");
            if (query == null) throw new ArgumentNullException("query");

            return this.Transaction<int>(collection, false, (col) =>
            {
                if (col == null) return 0;

                // define auto-index create factory if not exists
                query.IndexFactory((c, f) => this.EnsureIndex(c, f, false));

                var nodes = query.Run(col, _indexer);
                var count = 0;

                foreach (var node in nodes)
                {
                    _log.Write(Logger.COMMAND, "delete document on '{0}' :: _id = {1}", collection, node.Key);

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