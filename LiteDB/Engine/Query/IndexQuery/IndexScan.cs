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

        public IndexScan(string name, Func<BsonValue, bool> func, int order)
            : base(name, order)
        {
            _func = func;
        }

        internal override long GetCost(CollectionIndex index)
        {
            // index full scan
            return index.KeyCount;
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            return indexer
                .FindAll(index, this.Order)
                .Where(i => _func(i.Key));
        }

        public override string ToString()
        {
            return string.Format("FULL SCAN({0})", this.Name);
        }
    }
}