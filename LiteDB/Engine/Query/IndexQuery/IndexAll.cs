using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    /// <summary>
    /// Return all index nodes
    /// </summary>
    internal class IndexAll : Index
    {
        public IndexAll(string name, int order)
            : base(name, order)
        {
        }

        internal override uint GetCost(CollectionIndex index)
        {
            // always worst cost - return all documents with no index use (just for order)
            return index.KeyCount;
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            return indexer.FindAll(index, this.Order);
        }

        public override string ToString()
        {
            return string.Format("FULL SCAN({0})", this.Name);
        }
    }
}