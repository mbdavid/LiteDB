using LiteDB.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal partial class LiteEngine : IDisposable
    {
        public IEnumerable<BsonDocument> Find(string colName, Query query, int skip = 0, int limit = int.MaxValue)
        {
            // get my collection page
            var col = this.GetCollectionPage(colName, false);

            // no collection, no documents
            if(col == null) yield break;

            // get nodes from query executor to get all IndexNodes
            var nodes = query.Run(col, _indexer);

            // skip first N nodes
            if (skip > 0) nodes = nodes.Skip(skip);

            // limit in M nodes
            if (limit != int.MaxValue) nodes = nodes.Take(limit);

            // for each document, read data and deserialize as document
            foreach (var node in nodes)
            {
                var dataBlock = _data.Read(node.DataBlock, true);

                var doc = BsonSerializer.Deserialize(dataBlock.Buffer).AsDocument;

                yield return doc;
            }
        }
    }
}
