using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Find for documents in a collection using Query definition
        /// </summary>
        public IEnumerable<BsonDocument> Find(string collection, Query query, LiteTransaction transaction)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (query == null) throw new ArgumentNullException(nameof(query));

            // executing query
            IEnumerable<BsonDocument> execute(Snapshot snapshot)
            {
                var col = snapshot.CollectionPage;
                var data = new DataService(snapshot);
                var indexer = new IndexService(snapshot);

                // no collection, no documents
                if (col == null) yield break;

                // get node list from query
                var nodes = query.Index.Run(col, indexer);

                // load document from disk
                var docs = loadDocument(nodes, data, query.KeyOnly, query.Index.Name);

                // load pipe query to apply all query options
                var pipe = new QueryPipe();

                // call safepoint just before return each document
                foreach (var doc in pipe.Pipe(docs, query))
                {
                    transaction.Safepoint();

                    yield return doc;
                }
            }

            // load documents from disk or make a "fake" document using key only
            IEnumerable<BsonDocument> loadDocument(IEnumerable<IndexNode> nodes, DataService data, bool keyOnly, string name)
            {
                foreach(var node in nodes)
                {
                    if (keyOnly)
                    {
                        yield return new BsonDocument { [name] = node.Key };
                    }
                    else
                    {
                        var buffer = data.Read(node.DataBlock);
                        var doc = _bsonReader.Deserialize(buffer);

                        yield return doc;
                    }
                }
            }

            // start execution
            return transaction.CreateSnapshot(query.ForUpdate ? SnapshotMode.Write : SnapshotMode.Read, collection, false, snapshot =>
            {
                // encapsulate method because yield return do not work inside Func
                return execute(snapshot);
            });

        }
    }
}