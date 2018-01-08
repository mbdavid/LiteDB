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
        public IEnumerable<BsonDocument> Find(string collection, Query query)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (query == null) throw new ArgumentNullException(nameof(query));

            using (var trans = this.ReadTransaction(collection))
            {
                var col = trans.CollectionPage;

                // no collection, no documents
                if (col == null) yield break;

                // get node list from query
                var nodes = query.Run(col, trans.Indexer);

                foreach (var node in nodes)
                {
                    var buffer = trans.Data.Read(node.DataBlock);
                    var doc = _bsonReader.Deserialize(buffer).AsDocument;

                    // if query need filter document, filter now
                    if (query.UseFilter && query.FilterDocument(doc) == false) continue;

                    trans.Safepoint();

                    yield return doc;
                }
            }
        }
    }
}