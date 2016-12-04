using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal class QueryAnd : Query
    {
        private Query _left;
        private Query _right;

        public QueryAnd(Query left, Query right)
            : base(null)
        {
            _left = left;
            _right = right;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            throw new NotSupportedException();
        }

        internal override IEnumerable<IndexNode> Run(CollectionPage col, IndexService indexer)
        {
            // ignore QueryAll on AND expression (in both sides)
            if (_left is QueryAll) return _right.Run(col, indexer);
            if (_right is QueryAll) return _left.Run(col, indexer);

            var left = _left.Run(col, indexer);
            var right = _right.Run(col, indexer);

            return left.Intersect(right, new IndexNodeComparer());
        }
    }
}