using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Implement between operation - in asc or desc way - can be used as LT, LTE, GT, GTE too because support MinValue/MaxValue
    /// </summary>
    internal class IndexBetween : Index
    {
        private BsonValue _start;
        private BsonValue _end;

        private bool _startEquals;
        private bool _endEquals;

        public IndexBetween(string name, BsonValue start, BsonValue end, bool startEquals, bool endEquals, int order)
            : base(name, order)
        {
            // if order as desc, use swap start/end values
            _start = order == Query.Ascending ? start : end;
            _end = order == Query.Ascending ? end : start;

            _startEquals = order == Query.Ascending ? startEquals : endEquals;
            _endEquals = order == Query.Ascending ? endEquals : startEquals;
        }

        internal override double GetScore(CollectionIndex index)
        {
            // need some statistics here
            return 0.01;
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            // find first indexNode (or get from head/tail if Min/Max value)
            var node = 
                _start == BsonValue.MinValue ? indexer.GetNode(index.HeadNode) :
                _start == BsonValue.MaxValue ? indexer.GetNode(index.TailNode) :
                indexer.Find(index, _start, true, this.Order);

            // returns (or not) equals start value
            while (node != null)
            {
                var diff = node.Key.CompareTo(_start);

                // if current value are not equals start, go out this loop
                if (diff != 0) break;

                if (_startEquals && !node.IsHeadTail(index))
                {
                    yield return node;
                }

                node = indexer.GetNode(node.NextPrev(0, this.Order));
            }

            // navigate using next[0] do next node - if less or equals returns
            while (node != null)
            {
                var diff = node.Key.CompareTo(_end);

                if (_endEquals && diff == 0 && !node.IsHeadTail(index))
                {
                    yield return node;
                }
                else if (diff == -this.Order && !node.IsHeadTail(index))
                {
                    yield return node;
                }
                else
                {
                    break;
                }

                node = indexer.GetNode(node.NextPrev(0, this.Order));
            }
        }

        public override string ToString()
        {
            return string.Format("INTERVAL({0}) {1}", this.Name, this.Order == Query.Ascending ? "ASC" : "DESC");
        }
    }
}