using System;
using System.Collections.Generic;

namespace LiteDB
{
    /// <summary>
    /// Return all index nodes
    /// </summary>
    internal class IndexAll : Index
    {
        private int _order;

        public IndexAll(string name, int order)
            : base(name)
        {
            _order = order;
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            return indexer.FindAll(index, _order);
        }
    }
}