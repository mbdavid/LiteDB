using System;
using System.Collections.Generic;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement virtual index for system collections AND full data collection read
    /// </summary>
    internal class IndexVirtual : Index, IDocumentLookup
    {
        private readonly IEnumerable<BsonDocument> _source;

        private Dictionary<uint, BsonDocument> _cache = null;

        public IndexVirtual(IEnumerable<BsonDocument> source)
            : base(null, 0)
        {
            _source = source;
        }

        public override uint GetCost(CollectionIndex index)
        {
            // there is no way to determine how many document are inside _source without run Count() this
            return uint.MaxValue;
        }

        public override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IndexNode> Run(CollectionPage col, IndexService indexer)
        {
            var rawId = 0u;

            foreach(var doc in _source)
            {
                // create fake rawId for document source
                doc.RawId = new PageAddress(rawId, 0);

                rawId++;

                // return an fake indexNode
                yield return new IndexNode(doc);
            }
        }

        public BsonDocument Load(IndexNode node)
        {
            return node.Key as BsonDocument;
        }

        public BsonDocument Load(PageAddress rawId)
        {
            // if this method need to be used, let's add all documents in cache
            if (_cache == null)
            {
                //TODO: migrate do in disk cache
                _cache = this.Run(null, null)
                    .Select(x => x.Key.AsDocument)
                    .ToDictionary(x => x.RawId.PageID);
            }

            return _cache[rawId.PageID];
        }

        public override string ToString()
        {
            return string.Format("FULL COLLECTION SCAN");
        }
    }
}