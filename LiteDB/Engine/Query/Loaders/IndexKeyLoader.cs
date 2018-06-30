using System.Collections.Generic;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement loader based only in index Key
    /// </summary>
    internal class IndexKeyLoader : IDocumentLoader
    {
        private readonly IndexService _indexer;
        private readonly string _name;

        public IndexKeyLoader(IndexService indexer, string name)
        {
            _indexer = indexer;
            _name = name;
        }

        public BsonDocument Load(PageAddress rawId)
        {
            DEBUG(rawId.IsEmpty, "Never should be empty rawid");

            var node = _indexer.GetNode(rawId);

            var doc = new BsonDocument
            {
                [_name] = node.Key,
            };

            doc.RawId = rawId;

            return doc;
        }
    }
}