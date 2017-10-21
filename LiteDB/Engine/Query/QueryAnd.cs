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

        internal override bool UseFilter
        {
            get
            {
                // return true if any site use filter
                return _left.UseFilter || _right.UseFilter;
            }
            set
            {
                // set both sides with value
                _left.UseFilter = value;
                _right.UseFilter = value;
            }
        }

        internal override IEnumerable<IndexNode> Run(CollectionPage col, IndexService indexer)
        {
            // execute both run operation but not fetch any node yet
            var left = _left.Run(col, indexer);
            var right = _right.Run(col, indexer);

            // if left use index, force right use full scan (left has preference to use index)
            if (_left.UseIndex)
            {
                this.UseIndex = true;
                _right.UseFilter = true;
                return left;
            }

            // if right use index (and left no), force left use filter
            if (_right.UseIndex)
            {
                this.UseIndex = true;
                _left.UseFilter = true;
                return right;
            }

            // neither left and right uses index (both are full scan)
            this.UseIndex = false;
            this.UseFilter = true;

            return left.Intersect(right, new IndexNodeComparer());
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            throw new NotSupportedException();
        }

        internal override bool FilterDocument(BsonDocument doc)
        {
            return _left.FilterDocument(doc) && _right.FilterDocument(doc);
        }

        public override string ToString()
        {
            return string.Format("({0} and {1})", _left, _right);
        }
    }
}