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

        internal override double GetScore(CollectionIndex index)
        {
            // full index scan
            return 0;
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            return indexer
                .FindAll(index, this.Order)
                .Where(i => _func(i.Key));
        }

        public override string ToString()
        {
            return string.Format("SCAN({0}) {1}", this.Name, this.Order == Query.Ascending ? "ASC" : "DESC");
        }
    }
}