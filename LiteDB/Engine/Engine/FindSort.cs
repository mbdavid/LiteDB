using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Experimental Find with Sort operation
        /// </summary>
        public IEnumerable<BsonDocument> FindSort(string collection, Query query, string orderBy, int order = Query.Ascending, int skip = 0, int limit = int.MaxValue)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (query == null) throw new ArgumentNullException(nameof(query));

            _log.Write(Logger.COMMAND, "query-sort documents in '{0}' => {1}", collection, query);

            // lock database for read access
            using (_locker.Read())
            {
                // get collection page
                var col = this.GetCollectionPage(collection, false);

                if (col == null) return new List<BsonDocument>();

                // total documents keeps in memory to be sorted
                var total = limit == int.MaxValue ? int.MaxValue : skip + limit;
                var last = BsonValue.MaxValue;

                // resolve orderBy as an expression
                var expr = new BsonExpression(orderBy);

                // create sortedlist, in memory
                var sorted = new SortedSet<KeyDocument>(new KeyDocumentComparer());

                // first lets works only with index in query
                var nodes = query.Run(col, _indexer);

                foreach (var node in nodes)
                {
                    var buffer = _data.Read(node.DataBlock);
                    var doc = _bsonReader.Deserialize(buffer).AsDocument;

                    // if needs use filter
                    if (query.UseFilter && query.FilterDocument(doc) == false) continue;

                    // get key to be sorted
                    var key = expr.Execute(doc, true).First();
                    var diff = key.CompareTo(last);

                    // add to list only if lower than last space
                    if(diff < 1)
                    {
                        sorted.Add(new KeyDocument
                        {
                            Key = key,
                            Document = doc
                        });

                        // exceeded limit
                        if (sorted.Count > total)
                        {
                            var exceeded = sorted.ElementAt(total);

                            sorted.Remove(exceeded);

                            last = sorted.Last().Key;
                        }
                    }
                }

                return sorted
                    .Skip(skip)
                    .Select(x => x.Document);
            }
        }
    }
}