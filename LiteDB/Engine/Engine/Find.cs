using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            IEnumerable<BsonDocument> DoFind(Snapshot snapshot)
            {
                var col = snapshot.CollectionPage;
                var data = new DataService(snapshot);
                var indexer = new IndexService(snapshot);
                var loader = new DocumentLoader(data, _bsonReader);

                // no collection, no documents
                if (col == null) yield break;

                // get node list from query
                var nodes = query.Index.Run(col, indexer);

                // load document from disk
                var docs = LoadDocument(nodes, loader, query.KeyOnly, query.Index.Name);

                // load pipe query to apply all query options
                var pipe = new QueryPipe(this, transaction, loader);

                // call safepoint just before return each document
                foreach (var doc in pipe.Pipe(docs, query))
                //foreach (var doc in docs)
                {
                    transaction.Safepoint();

                    yield return doc;
                }
            }

            // load documents from disk or make a "fake" document using key only (useful for COUNT/EXISTS)
            IEnumerable<BsonDocument> LoadDocument(IEnumerable<IndexNode> nodes, IDocumentLoader loader, bool keyOnly, string name)
            {
                foreach (var node in nodes)
                {
                    yield return keyOnly ?
                        new BsonDocument { [name] = node.Key } :
                        loader.Load(node.DataBlock);
                }
            }

            // call DoFind inside read-only snapshot
            return transaction.CreateSnapshot(SnapshotMode.Read, collection, false, snapshot =>
            {
                return DoFind(snapshot);
            });
        }
    }
}