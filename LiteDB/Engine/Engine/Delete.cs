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
        public int Delete(string collection, IEnumerable<BsonValue> ids, LiteTransaction transaction)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            return transaction.CreateSnapshot(SnapshotMode.Write, collection, false, snapshot =>
            {
                var col = snapshot.CollectionPage;
                var data = new DataService(snapshot);
                var indexer = new IndexService(snapshot);

                if (col == null) return 0;

                var count = 0;
                var pk = col.PK;

                foreach(var id in ids)
                {
                    var pkNode = indexer.Find(pk, id, false, Index.Ascending);

                    // if pk not found, continue
                    if (pkNode == null) continue;

                    // get all indexes nodes from this data block
                    var allNodes = indexer.GetNodeList(pkNode, true).ToArray();

                    // lets remove all indexes that point to this in dataBlock
                    foreach (var linkNode in allNodes)
                    {
                        var index = col.GetIndex(linkNode.Slot);

                        indexer.Delete(index, linkNode.Position);
                    }

                    // remove object data
                    data.Delete(col, pkNode.DataBlock);

                    transaction.Safepoint();

                    count++;
                }

                return count;
            });
        }
    }
}