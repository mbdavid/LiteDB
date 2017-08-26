using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal class QueryGreater : Query
    {
        private BsonValue _value;
        private bool _equals;

        public BsonValue Value { get { return _value; } }
        public bool IsEquals { get { return _equals; } }

        public QueryGreater(string field, BsonValue value, bool equals)
            : base(field)
        {
            _value = value;
            _equals = equals;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            // find first indexNode
            var node = indexer.Find(index, _value, true, Query.Ascending);

            if (node == null) yield break;

            // move until next is last
            while (node != null)
            {
                // compares only with are same type
                if (node.Key.Type == _value.Type || (node.Key.IsNumber && _value.IsNumber))
                {
                    var diff = node.Key.CompareTo(_value);

                    if (diff == 1 || (_equals && diff == 0))
                    {
                        if (node.IsHeadTail(index)) yield break;

                        yield return node;
                    }
                }

                node = indexer.GetNode(node.Next[0]);
            }
        }

        internal override bool FilterDocument(BsonDocument doc)
        {
            return this.Expression.Execute(doc, true)
                .Where(x => x.Type == _value.Type || (x.IsNumber && _value.IsNumber))
                .Any(x => x.CompareTo(_value) >= (_equals ? 0 : 1));
        }

        public override string ToString()
        {
            return string.Format("{0}({1} >{2} {3})",
                this.UseFilter ? "Filter" : this.UseIndex ? "Seek" : "",
                this.Expression?.ToString() ?? this.Field,
                _equals ? "=" : "",
                _value);
        }
    }
}