using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Implements delete based on IDs enumerable
        /// </summary>
        public int Delete(string collection, IEnumerable<BsonValue> ids)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (ids == null) throw new ArgumentNullException(nameof(ids));

            return this.WriteTransaction(TransactionMode.Write, collection, false, trans =>
            {
                var col = trans.CollectionPage;

                if (col == null) return 0;

                var count = 0;
                var pk = col.PK;

                foreach(var id in ids)
                {
                    var pkNode = trans.Indexer.Find(pk, id, false, Query.Ascending);

                    // if pk not found, continue
                    if (pkNode == null) continue;

                    // get all indexes nodes from this data block
                    var allNodes = trans.Indexer.GetNodeList(pkNode, true).ToArray();

                    // lets remove all indexes that point to this in dataBlock
                    foreach (var linkNode in allNodes)
                    {
                        var index = col.GetIndex(linkNode.Slot);

                        trans.Indexer.Delete(index, linkNode.Position);
                    }

                    // remove object data
                    trans.Data.Delete(col, pkNode.DataBlock);

                    trans.Safepoint();

                    count++;
                }

                return count;
            });
        }
    }
}