using System;
using System.Collections.Generic;

namespace LiteDB
{
    internal class QueryLess : Query
    {
        private BsonValue _value;
        private bool _equals;

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
                var diff = node.Key.CompareTo(_value);

                if (diff == 1 || (!_equals && diff == 0)) break;

                if (node.IsHeadTail(index)) yield break;

                yield return node;
            }
        }

        internal override bool ExecuteFullScan(BsonDocument doc)
        {
            return doc.Get(this.Field).CompareTo(_value) <= (_equals ? 0 : -1);
        }

        public override string ToString()
        {
            return string.Format("{0} <{1} {2}", this.Field, _equals ? "=" : "", _value);
        }
    }
}