using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal class QueryStringEquals : Query
    {
        private BsonValue _value;
        private bool _ignoreCase;

        public QueryStringEquals(string field, BsonValue value, bool ignoreCase)
            : base(field)
        {
            _value = value;
            _ignoreCase = ignoreCase;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            var node = indexer.Find(index, _value, false, Query.Ascending);

            if (node == null) yield break;

            yield return node;

            if (index.Unique == false)
            {
                // navigate using next[0] do next node - if equals, returns
                while (!node.Next[0].IsEmpty && ((node = indexer.GetNode(node.Next[0])).Key.CompareTo(_value) == 0))
                {
                    if (node.IsHeadTail(index)) yield break;

                    yield return node;
                }
            }
        }

        internal override bool FilterDocument(BsonDocument doc)
            => Expression.Execute(doc, true).Any(x => string.Equals(_value.AsString, x.AsString, _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));

        public override string ToString()
            => $"{(UseFilter ? "Filter" : UseIndex ? "Seek" : "")}({Expression?.ToString() ?? Field} = {_value})";
    }
}
