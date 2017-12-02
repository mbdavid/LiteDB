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
        public List<BsonDocument> FindSort(string collection, Query query, string orderBy, int order = Query.Ascending, int skip = 0, int limit = int.MaxValue)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (query == null) throw new ArgumentNullException(nameof(query));

            _log.Write(Logger.COMMAND, "query-sort documents in '{0}' => {1}", collection, query);

            // evaluate orderBy path/expression
            var expr = new BsonExpression(orderBy);

            // lock database for read access
            using (_locker.Read())
            {
                // create memory database
                using (var engine = new LiteEngine(new MemoryStream()))
                {
                    // get collection page
                    var col = this.GetCollectionPage(collection, false);

                    if (col == null) return new List<BsonDocument>();

                    // create a temp collection in new memory database
                    var tmp = engine._collections.Add("tmp");

                    // create index pointer
                    var index = engine._indexer.CreateIndex(tmp);

                    index.Field = "s";
                    index.Expression = "$.s";
                    index.Unique = false;

                    // first lets works only with index in query
                    var nodes = query.Run(col, _indexer);

                    foreach (var node in nodes)
                    {
                        var buffer = _data.Read(node.DataBlock);
                        var doc = _bsonReader.Deserialize(buffer).AsDocument;

                        // if needs use filter
                        if (query.UseFilter && query.FilterDocument(doc) == false) continue;

                        // get sort value to be indexed
                        var key = expr.Execute(doc, true).First();

                        var tmpNode = engine._indexer.AddNode(index, key, null);

                        tmpNode.DataBlock = node.DataBlock;
                    }

                    var result = new List<BsonDocument>();

                    // now I have an index in sorted field
                    // apply skip/take before get
                    foreach(var node in engine._indexer.FindAll(index, order).Skip(skip).Take(limit))
                    {
                        var buffer = _data.Read(node.DataBlock);
                        var doc = _bsonReader.Deserialize(buffer).AsDocument;

                        result.Add(doc);
                    }

                    return result;
                }
            }
        }
    }
}