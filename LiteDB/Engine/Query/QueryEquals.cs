using System;
using System.Linq;
using System.Collections.Generic;

namespace LiteDB
{
    internal class QueryEquals : Query
    {
        private BsonValue _value;

        public QueryEquals(string field, BsonValue value)
            : base(field)
        {
            _value = value;
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
        {
            return this.Expression.Execute(doc, true)
                .Any(x => x.CompareTo(_value) == 0);
        }

        public override string ToString()
        {
            return string.Format("{0}({1} = {2})",
                this.UseFilter ? "Filter" : this.UseIndex ? "Seek" : "",
                this.Expression?.ToString() ?? this.Field,
                _value);
        }
    }
}