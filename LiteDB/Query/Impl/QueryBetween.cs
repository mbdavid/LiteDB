using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryBetween : Query
    {
        private BsonValue _start;
        private BsonValue _end;

        public QueryBetween(string field, BsonValue start, BsonValue end)
            : base(field)
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

            // if end value - start value was normilized on Find method
            _end.Normalize(index.Options);

            // navigate using next[0] do next node - if less or equals returns
            while (node != null)
            {
                var diff = node.Value.CompareTo(_end);

                if (diff == 0 || diff != order)
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
