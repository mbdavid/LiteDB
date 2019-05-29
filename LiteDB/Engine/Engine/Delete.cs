using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
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

            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(LockMode.Write, collection, false);
                var col = snapshot.CollectionPage;
                var data = new DataService(snapshot);
                var indexer = new IndexService(snapshot);

                if (snapshot.CollectionPage == null) return 0;

                var count = 0;
                var pk = snapshot.CollectionPage.PK;

                foreach (var id in ids)
                {
                    var pkNode = indexer.Find(pk, id, false, LiteDB.Query.Ascending);

                    // if pk not found, continue
                    if (pkNode == null) continue;

                    // delete all nodes (start in pk node)
                    indexer.DeleteAll(pkNode.Position);

                    // remove object data
                    data.Delete(pkNode.DataBlock);

                    transaction.Safepoint();

                    count++;
                }

                return count;
            });
        }

        /// <summary>
        /// Implements delete based on filter expression
        /// </summary>
        public int DeleteMany(string collection, BsonExpression predicate)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return this.AutoTransaction(transaction =>
            {
                // do optimization for when using "_id = value" key
                if (predicate.Type == BsonExpressionType.Equal && 
                    predicate.Left.Type == BsonExpressionType.Path && 
                    predicate.Left.Source == "$._id" && 
                    predicate.Right.IsValue)
                {
                    var id = predicate.Right.Execute().First();

                    return this.Delete(collection, new BsonValue[] { id });
                }
                else
                {
                    IEnumerable<BsonValue> getIds()
                    {
                        var query = new Query { Select = "_id" };

                        query.Where.Add(predicate);

                        using (var reader = this.Query(collection, query))
                        {
                            while (reader.Read())
                            {
                                yield return reader.Current.AsDocument["_id"];
                            }
                        }
                    }

                    return this.Delete(collection, getIds());
                }
            });
        }
    }
}