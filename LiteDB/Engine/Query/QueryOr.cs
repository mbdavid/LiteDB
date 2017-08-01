using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal class QueryOr : Query
    {
        private Query _left;
        private Query _right;

        public QueryOr(Query left, Query right)
            : base(null)
        {
            _left = left;
            _right = right;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            throw new NotSupportedException();
        }

        internal override bool ExecuteFullScan(BsonDocument doc)
        {
            return _left.ExecuteFullScan(doc) || _right.ExecuteFullScan(doc);
        }

        internal override IEnumerable<IndexNode> Run(CollectionPage col, IndexService indexer)
        {
            var left = _left.Run(col, indexer);
            var right = _right.Run(col, indexer);

            // if any query (left/right) is FullScan, this query is full scan too
            this.Mode = _left.Mode == QueryMode.FullScan || _right.Mode == QueryMode.FullScan ? QueryMode.FullScan : QueryMode.IndexSeek;

            return left.Union(right, new IndexNodeComparer());
        }

        public override string ToString()
        {
            return string.Format("({0} or {1})", _left, _right);
        }
    }
}