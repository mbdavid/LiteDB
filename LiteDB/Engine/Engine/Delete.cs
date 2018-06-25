using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Implement delete command based on _id value. Returns true if deleted
        /// </summary>
        public bool Delete(string collection, BsonValue id)
        {
            return this.Delete(collection, new[] { id }) == 1;
        }

        /// <summary>
        /// Implements delete based on IDs enumerable
        /// </summary>
        public int Delete(string collection, IEnumerable<BsonValue> ids)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (ids == null) throw new ArgumentNullException(nameof(ids));

            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(LockMode.Write, collection, false);
                var col = snapshot.CollectionPage;
                var data = new DataService(snapshot);
                var indexer = new IndexService(snapshot);

                if (col == null) return 0;

                var count = 0;
                var pk = col.PK;

                foreach (var id in ids)
                {
                    var pkNode = indexer.Find(pk, id, false, LiteDB.Query.Ascending);

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

        /// <summary>
        /// Implements delete based on filter expression
        /// </summary>
        public int Delete(string collection, BsonExpression where)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (where == null) throw new ArgumentNullException(nameof(where));

            return this.AutoTransaction(transaction =>
            {
                // do optimization for when using "_id = constant" key
                if (where.Type == BsonExpressionType.Equal && 
                    where.Left.Type == BsonExpressionType.Path && 
                    where.Left.Source == "$._id" && 
                    where.Right.IsConstant)
                {
                    var id = where.Right.Execute().First();

                    return this.Delete(collection, new BsonValue[] { id });
                }
                else
                {
                    var q = this.Query(collection)
                        .Where(where)
                        .Select("_id")
                        .ForUpdate()
                        .ToValues();

                    return this.Delete(collection, q);
                }
            });
        }
    }
}