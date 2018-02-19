using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Execute all indexes and return all results
    /// </summary>
    internal class IndexOr : Index
    {
        private List<Index> _indexes;

        public IndexOr(List<Index> indexes)
            : base("OR", Query.Ascending)
        {
            _indexes = indexes;
        }

        internal override double GetScore(CollectionIndex index)
        {
            // for OR, return an average from all
            return _indexes.Average(x => x.GetScore(index));
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            throw new NotSupportedException();
        }

        internal override IEnumerable<IndexNode> Run(CollectionPage col, IndexService indexer)
        {
            foreach(var index in _indexes)
            {
                foreach(var node in index.Run(col, indexer))
                {
                    yield return node;
                }
            }
        }

        public override string ToString()
        {
            return string.Format("OR({0} : {1})", _indexes.FirstOrDefault()?.ToString(), _indexes.Count);
        }
    }
}