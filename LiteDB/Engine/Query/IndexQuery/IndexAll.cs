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

        public override string ToString()
        {
            return string.Format("ALL({0}, {1})", this.Name,  _order);
        }
    }
}