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
        internal override IEnumerable<IndexNode> Execute(LiteDatabase db, CollectionIndex index)
        {
            return null;
        }

        internal override IEnumerable<IndexNode> Run(LiteDatabase db, CollectionPage col)
        {
            var left = this.Left.Run(db, col);
            var right = this.Right.Run(db, col);

            return left.Union(right, new IndexNodeComparer());
        }
    }
}
