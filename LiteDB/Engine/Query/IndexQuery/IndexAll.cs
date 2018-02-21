using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
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

        internal override double GetScore(CollectionIndex index)
        {
            // full index scan - worst case
            return 0;
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