using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal class QueryLess : Query
    {
        private BsonValue _value;
        private bool _equals;

        public BsonValue Value { get { return _value; } }
        public bool IsEquals { get { return _equals; } }

        public QueryLess(string field, BsonValue value, bool equals)
            : base(field)
        {
            _value = value;
            _equals = equals;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            foreach (var node in indexer.FindAll(index, Query.Ascending))
            {
                // compares only with are same type
                if (node.Key.Type == _value.Type || (node.Key.IsNumber && _value.IsNumber))
                {
                    var diff = node.Key.CompareTo(_value);

                    if (diff == 1 || (!_equals && diff == 0)) break;

                    if (node.IsHeadTail(index)) yield break;

                    yield return node;
                }
            }
        }

        internal override bool FilterDocument(BsonDocument doc)
        {
            return this.Expression.Execute(doc, true)
                .Where(x => x.Type == _value.Type || (x.IsNumber && _value.IsNumber))
                .Any(x => x.CompareTo(_value) <= (_equals ? 0 : -1));
        }

        public override string ToString()
        {
            return string.Format("{0}({1} <{2} {3})",
                this.UseFilter ? "Filter" : this.UseIndex ? "Seek" : "",
                this.Expression?.ToString() ?? this.Field,
                _equals ? "=" : "",
                _value);
        }
    }
}