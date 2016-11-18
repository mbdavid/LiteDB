using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
   public partial class DbEngine
    {
        /// <summary>
        /// Find for documents in a collection using Query definition
        /// </summary>
        public IEnumerable<BsonDocument> Find(string colName, Query query, int skip = 0, int limit = int.MaxValue)
        {
            // transaction will be closed as soon as the IEnumerable goes out of scope
            using (var trans = _transaction.Begin(true))
            {
                // get my collection page
                var col = this.GetCollectionPage(colName, false);

                // no collection, no documents
                if (col == null) yield break;

                // get nodes from query executor to get all IndexNodes
                IEnumerable<IndexNode> nodes;

                try
                {
                    nodes = query.Run(col, _indexer);
                }
                catch (Exception ex)
                {
                    _log.Write(Logger.ERROR, ex.Message);
                    trans.Rollback();
                    throw;
                }

                // skip first N nodes
                if (skip > 0) nodes = nodes.Skip(skip);

                // limit in M nodes
                if (limit != int.MaxValue) nodes = nodes.Take(limit);

                // for each document, read data and deserialize as document
                foreach (var node in nodes)
                {
                    _log.Write(Logger.QUERY, "read document on '{0}' :: _id = {1}", colName, node.Key);

                    byte[] buffer;
                    BsonDocument doc;

                    // encapsulate read operation inside a try/catch (yeild do not support try/catch)
                    try
                    {
                        buffer = _data.Read(node.DataBlock);
                        doc = BsonSerializer.Deserialize(buffer).AsDocument;
                    }
                    catch (Exception ex)
                    {
                        _log.Write(Logger.ERROR, ex.Message);
                        trans.Rollback();
                        throw;
                    }

                    yield return doc;

                    _cache.CheckPoint();
                }
            }
        }
    }
}