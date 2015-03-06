using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

        // Never runs in AND/OR queries
        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            throw new Exception("Never used in AND/OR operations");
        }

        internal override IEnumerable<IndexNode> Run<T>(LiteCollection<T> collection)
        {
            var left = _left.Run(collection);
            var right = _right.Run(collection);

            return left.Union(right, new IndexNodeComparer());
        }
    }
}
