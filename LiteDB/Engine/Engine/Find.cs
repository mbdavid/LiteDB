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

            return transaction.CreateSnapshot(SnapshotMode.Read, collection, false, snapshot =>
            {
                var col = snapshot.CollectionPage;
                var data = new DataService(snapshot);
                var indexer = new IndexService(snapshot);
                var docs = new List<BsonDocument>();

                // no collection, no documents
                if (col == null) return docs;

                // get node list from query
                var nodes = query.Run(col, indexer);

                foreach (var node in nodes)
                {
                    var buffer = data.Read(node.DataBlock);
                    var doc = _bsonReader.Deserialize(buffer).AsDocument;

                    // if query need filter document, filter now
                    if (query.UseFilter && query.FilterDocument(doc) == false) continue;

                    transaction.Safepoint();

                    docs.Add(doc);
                }

                return docs;
            });
        }
    }
}