using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Implement between operation
    /// </summary>
    internal class IndexBetween : Index
    {
        private BsonValue _start;
        private BsonValue _end;

        private bool _startEquals;
        private bool _endEquals;

        public IndexBetween(string name, BsonValue start, BsonValue end, bool startEquals, bool endEquals)
            : base(name)
        {
            _start = start;
            _startEquals = startEquals;
            _end = end;
            _endEquals = endEquals;
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            // define order
            var order = _start.CompareTo(_end) <= 0 ? Query.Ascending : Query.Descending;

            // find first indexNode
            var node = indexer.Find(index, _start, true, order);

            // returns (or not) equals start value
            while (node != null)
            {
                var diff = node.Key.CompareTo(_start);

                // if current value are not equals start, go out this loop
                if (diff != 0) break;

                if (_startEquals)
                {
                    yield return node;
                }

                node = indexer.GetNode(node.NextPrev(0, order));
            }

            // navigate using next[0] do next node - if less or equals returns
            while (node != null)
            {
                var diff = node.Key.CompareTo(_end);

                if (_endEquals && diff == 0)
                {
                    yield return node;
                }
                else if (diff == -order)
                {
                    yield return node;
                }
                else
                {
                    break;
                }

                node = indexer.GetNode(node.NextPrev(0, order));
            }
        }
    }
}