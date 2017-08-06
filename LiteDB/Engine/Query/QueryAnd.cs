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

        internal override void ForceUseFilter()
        {
            _left.ForceUseFilter();
            _right.ForceUseFilter();
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            // will never run this because I override "Run" method
            throw new NotSupportedException();
        }

        internal override bool FilterDocument(BsonDocument doc)
        {
            return _left.FilterDocument(doc) && _right.FilterDocument(doc);

            //return
            //    (_left.UseFilter && _right.UseFilter) ? _left.FilterDocument(doc) && _right.FilterDocument(doc) :
            //    _left.UseFilter ? _left.FilterDocument(doc) :
            //    _right.UseFilter ? _right.FilterDocument(doc) : false;
        }

        internal override IEnumerable<IndexNode> Run(CollectionPage col, IndexService indexer)
        {
            var left = _left.Run(col, indexer);

            // if left use index, force right use full scan
            if (_left.UseIndex)
            {
                this.UseIndex = true;
                this.UseFilter = true;
                _right.ForceUseFilter();
                return left;
            }

            var right = _right.Run(col, indexer);

            // if right use index (and left no), force left use filter
            if (_right.UseIndex)
            {
                this.UseIndex = true;
                this.UseFilter = true;
                _left.ForceUseFilter();
                return right;
            }

            // neither left and right uses index (both are full scan)
            this.UseIndex = false;
            this.UseFilter = true;

            return left.Intersect(right, new IndexNodeComparer());
        }

        public override string ToString()
        {
            return string.Format("({0} and {1})", _left, _right);
        }
    }
}