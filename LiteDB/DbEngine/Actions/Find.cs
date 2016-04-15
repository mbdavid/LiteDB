using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal partial class DbEngine : IDisposable
    {
        /// <summary>
        /// Find for documents in a collection using Query definition
        /// </summary>
        public IEnumerable<BsonDocument> Find(string colName, Query query, int skip = 0, int limit = int.MaxValue)
        {
            // get my collection page
            var col = this.GetCollectionPage(colName, false);

            // no collection, no documents
            if (col == null) yield break;

            // get nodes from query executor to get all IndexNodes
            var nodes = query.Run(col, _indexer);

            // skip first N nodes
            if (skip > 0) nodes = nodes.Skip(skip);

            // limit in M nodes
            if (limit != int.MaxValue) nodes = nodes.Take(limit);

            // for each document, read data and deserialize as document
            foreach (var node in nodes)
            {
                _log.Write(Logger.QUERY, "read document on '{0}' :: _id = {1}", colName, node.Key);

                var buffer = _data.Read(node.DataBlock);

                var doc = BsonSerializer.Deserialize(buffer).AsDocument;

                yield return doc;

                _cache.CheckPoint();
            }
        }
    }
}