using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Execute an "index scan" passing a Func as where
    /// </summary>
    internal class IndexScan : Index
    {
        private Func<BsonValue, bool> _func;
        private int _order;

        public IndexScan(string name, Func<BsonValue, bool> func, int order)
            : base(name)
        {
            _func = func;
            _order = order;
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            return indexer
                .FindAll(index, _order)
                .Where(i => _func(i.Key));
        }

        public override string ToString()
        {
            return string.Format("SCAN({0})", this.Name);
        }
    }
}