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
        public IEnumerable<BsonDocument> Find(string collection, Query query, int skip = 0, int limit = int.MaxValue)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (query == null) throw new ArgumentNullException(nameof(query));

            _log.Write(Logger.COMMAND, "query documents in '{0}' => {1}", collection, query);

            using (var cursor = new QueryCursor(query, skip, limit))
            {
                using (_locker.Read())
                {
                    // get my collection page
                    var col = this.GetCollectionPage(collection, false);

                    // no collection, no documents
                    if (col == null) yield break;

                    // get nodes from query executor to get all IndexNodes
                    cursor.Initialize(query.Run(col, _indexer).GetEnumerator());

                    _log.Write(Logger.QUERY, "{0} :: {1}", collection, query);

                    // fill buffer with documents 
                    cursor.Fetch(_trans, _data, _bsonReader);
                }

                // returing first documents in buffer
                foreach (var doc in cursor.Documents) yield return doc;

                // if still documents to read, continue
                while (cursor.HasMore)
                {
                    // lock read mode
                    using (var l = _locker.Read())
                    {
                        // if file was changed, re-run query and skip already returned documents
                        if (l.Changed)
                        {
                            var col = this.GetCollectionPage(collection, false);
                            
                            if (col == null) yield break;
                            
                            cursor.ReQuery(query.Run(col, _indexer).GetEnumerator());
                        }

                        cursor.Fetch(_trans, _data, _bsonReader);
                    }

                    // return documents from buffer
                    foreach (var doc in cursor.Documents) yield return doc;
                }
            }
        }

        #region FindOne/FindById

        /// <summary>
        /// Find first or default document based in collection based on Query filter
        /// </summary>
        public BsonDocument FindOne(string collection, Query query)
        {
            return this.Find(collection, query).FirstOrDefault();
        }

        /// <summary>
        /// Find first or default document based in _id field
        /// </summary>
        public BsonDocument FindById(string collection, BsonValue id)
        {
            if (id == null || id.IsNull) throw new ArgumentNullException(nameof(id));

            return this.Find(collection, Query.EQ("_id", id)).FirstOrDefault();
        }


        /// <summary>
        /// Returns all documents inside collection order by _id index.
        /// </summary>
        public IEnumerable<BsonDocument> FindAll(string collection)
        {
            return this.Find(collection, Query.All());
        }

        #endregion
    }
}