using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryLess : Query
    {
        private BsonValue _value;
        private bool _equals;
        private int _order;

        public QueryLess(string field, BsonValue value, bool equals, int order)
            : base(field)
        {
            _value = value;
            _equals = equals;
            _order = order;
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            return _order == Query.Ascending ? this.AscOrder(indexer, index) : this.DescOrder(indexer, index);
        }

        private IEnumerable<IndexNode> AscOrder(IndexService indexer, CollectionIndex index)
        {
            // normailize string (because are not using Find method)
            _value.Normalize(index.Options);

            foreach (var node in indexer.FindAll(index, Query.Ascending))
            {
                var diff = node.Value.CompareTo(_value);

                if (diff == 1 || (!_equals && diff == 0)) break;

                if (node.IsHeadTail) yield break;

                yield return node;
            }
        }

        private IEnumerable<IndexNode> DescOrder(IndexService indexer, CollectionIndex index)
        {
            // find first indexNode
            var node = indexer.Find(index, _value, true, Query.Descending);

            if (node == null) yield break;

            // move until next is last
            while (node != null)
            {
                var diff = node.Value.CompareTo(_value);

                if (diff == -1 || (_equals && diff == 0))
                {
                    if (node.IsHeadTail) yield break;

                    yield return node;
                }

                node = indexer.GetNode(node.Prev[0]);
            }
        }
    }
}
