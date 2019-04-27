using System;
using System.Collections.Generic;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement virtual index for system collections AND full data collection read
    /// </summary>
    internal class IndexVirtual : Index, IDocumentLoader
    {
        private readonly IEnumerable<BsonDocument> _source;

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
            foreach(var doc in _source)
            {
                // return an fake indexNode
                yield return new IndexNode(doc);
            }
        }

        public BsonDocument Load(IndexNode node)
        {
            return node.Key as BsonDocument;
        }

        public override string ToString()
        {
            return string.Format("FULL COLLECTION SCAN");
        }
    }
}