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

        internal override bool FilterDocument(BsonDocument doc)
        {
            return _left.FilterDocument(doc) && _right.FilterDocument(doc);
        }

        internal override IEnumerable<IndexNode> Run(CollectionPage col, IndexService indexer)
        {
            var left = _left.Run(col, indexer);
            var right = _right.Run(col, indexer);

            // if any query (left/right) is FullScan, this query is full scan too
            this.RunMode = _left.RunMode == QueryMode.Fullscan || _right.RunMode == QueryMode.Fullscan ? QueryMode.Fullscan : QueryMode.Index;

            return left.Intersect(right, new IndexNodeComparer());
        }

        public override string ToString()
        {
            return string.Format("({0} and {1})", _left, _right);
        }
    }
}