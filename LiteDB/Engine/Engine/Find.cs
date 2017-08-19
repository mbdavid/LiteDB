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
        public IEnumerable<BsonDocument> Find(string collection, Query query, int skip = 0, int limit = int.MaxValue, int bufferSize = 200)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException("collection");
            if (query == null) throw new ArgumentNullException("query");

            var docs = new List<BsonDocument>(bufferSize);

            using(var context = new QueryContext(query, skip, limit, bufferSize))
            {
                using (_locker.Read())
                {
                    // get my collection page
                    var col = this.GetCollectionPage(collection, false);

                    // no collection, no documents
                    if (col == null) yield break;

                    // get nodes from query executor to get all IndexNodes
                    context.Nodes = query.Run(col, _indexer).GetEnumerator();

                    _log.Write(Logger.QUERY, "{0} :: {1} ({2})", collection, query, context.Skip);

                    // fill buffer with documents 
                    docs.AddRange(context.GetDocuments(_trans, _data, _log));
                }

                // returing first documents in buffer
                foreach (var doc in docs) yield return doc;

                // if still documents to read, continue
                while (context.HasMore)
                {
                    // clear buffer
                    docs.Clear();

                    // lock read mode
                    using (_locker.Read())
                    {
                        // run query again skiping already returned documents
                        // it's important because collection could be changed between on lock and other
                        var col = this.GetCollectionPage(collection, false);

                        if (col == null) yield break;

                        context.Nodes = query.Run(col, _indexer).GetEnumerator();

                        _log.Write(Logger.QUERY, "{0} :: {1} ({2})", collection, query, context.Skip);

                        docs.AddRange(context.GetDocuments(_trans, _data, _log));
                    }

                    // return documents from buffer
                    foreach (var doc in docs) yield return doc;
                }
            }
        }

        /// <summary>
        /// Find index keys from collection. Do not retorn document, only key value
        /// </summary>
        public IEnumerable<BsonValue> FindIndex(string collection, Query query, int skip = 0, int limit = int.MaxValue, int bufferSize = 2000)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException("collection");
            if (query == null) throw new ArgumentNullException("query");

            var keys = new List<BsonValue>(bufferSize);

            using (var context = new QueryContext(query, skip, limit, bufferSize))
            {
                using (_locker.Read())
                {
                    // get my collection page
                    var col = this.GetCollectionPage(collection, false);

                    // no collection, no values
                    if (col == null) yield break;

                    // get nodes from query executor to get all IndexNodes
                    context.Nodes = query.Run(col, _indexer).GetEnumerator();

                    // FindIndex must run as Index seek (not by full scan)
                    if (!query.UseIndex) throw LiteException.IndexNotFound(collection, query.Field);

                    _log.Write(Logger.QUERY, "{0} :: {1} ({2})", collection, query, context.Skip);

                    // fill buffer with index keys
                    keys.AddRange(context.GetIndexKeys(_trans, _log));
                }

                // returing first keys in buffer
                foreach (var key in keys) yield return key;

                // if still documents to read, continue
                while (context.HasMore)
                {
                    // clear buffer
                    keys.Clear();

                    // lock read mode
                    using (_locker.Read())
                    {
                        // run query again skiping already returned documents
                        // it's important because collection could be changed between on lock and other
                        var col = this.GetCollectionPage(collection, false);

                        if (col == null) yield break;

                        context.Nodes = query.Run(col, _indexer).GetEnumerator();

                        if (!query.UseIndex) throw LiteException.IndexNotFound(collection, query.Field);

                        _log.Write(Logger.QUERY, "{0} :: {1} ({2})", collection, query, context.Skip);

                        keys.AddRange(context.GetIndexKeys(_trans, _log));
                    }

                    // return keys from buffer
                    foreach (var key in keys) yield return key;
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
            if (id == null || id.IsNull) throw new ArgumentNullException("id");

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