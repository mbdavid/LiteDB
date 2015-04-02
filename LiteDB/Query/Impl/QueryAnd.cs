using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

        internal override void NormalizeValues(IndexOptions options)
        {
            throw new NotSupportedException();
        }

        internal override bool ExecuteFullScan(BsonDocument doc, IndexOptions options)
        {
            return _left.ExecuteFullScan(doc, options) && _right.ExecuteFullScan(doc, options);
        }

        internal override IEnumerable<IndexNode> Run<T>(LiteCollection<T> collection)
        {
            var left = _left.Run(collection);
            var right = _right.Run(collection);

            // if any query (left/right) is FullScan, this query is full scan too
            this.ExecuteMode = _left.ExecuteMode == QueryExecuteMode.FullScan || _right.ExecuteMode == QueryExecuteMode.FullScan ? QueryExecuteMode.FullScan : QueryExecuteMode.IndexSeek;

            return left.Intersect(right, new IndexNodeComparer());
        }
    }
}
