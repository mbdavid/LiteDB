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

        public IndexBetween(string name, BsonValue start, BsonValue end)
            : base(name)
        {
            _start = start;
            _end = end;
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

                yield return node;

                node = indexer.GetNode(node.NextPrev(0, order));
            }

            // navigate using next[0] do next node - if less or equals returns
            while (node != null)
            {
                var diff = node.Key.CompareTo(_end);

                if (diff == 0 || diff == -order)
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

        public override string ToString()
        {
            return string.Format("BETWEEN({0})", this.Name);
        }
    }
}