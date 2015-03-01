using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryOr : Query
    {
        public Query Left { get; private set; }
        public Query Right { get; private set; }

        public QueryOr(Query left, Query right)
            : base(null)
        {
            this.Left = left;
            this.Right = right;
        }

        // Never runs in AND/OR queries
        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            return null;
        }

        internal override IEnumerable<IndexNode> Run<T>(LiteCollection<T> collection, CollectionPage col)
        {
            var left = this.Left.Run(collection, col);
            var right = this.Right.Run(collection, col);

            return left.Union(right, new IndexNodeComparer());
        }
    }
}
