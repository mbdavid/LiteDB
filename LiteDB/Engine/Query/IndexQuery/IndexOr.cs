using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    /// <summary>
    /// Execute all indexes and return all results
    /// </summary>
    internal class IndexOr : Index
    {
        private List<Index> _indexes;

        public IndexOr(List<Index> indexes)
            : base("OR", indexes.FirstOrDefault()?.Order ?? Query.Ascending)
        {
            _indexes = indexes;
        }

        internal override uint GetCost(CollectionIndex index)
        {
            return (uint)_indexes.Sum(x => x.GetCost(index));
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
            return string.Join(" OR ", _indexes.Select(x => x.ToString()));
        }
    }
}