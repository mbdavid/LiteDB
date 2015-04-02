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

        public QueryLess(string field, BsonValue value, bool equals)
            : base(field)
        {
            _value = value;
            _equals = equals;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            var value = _value.Normalize(index.Options);

            foreach (var node in indexer.FindAll(index, Query.Ascending))
            {
                var diff = node.Key.CompareTo(value);

                if (diff == 1 || (!_equals && diff == 0)) break;

                if (node.IsHeadTail(index)) yield break;

                yield return node;
            }
        }

        internal override void NormalizeValues(IndexOptions options)
        {
            _value = _value.Normalize(options);
        }

        internal override bool ExecuteFullScan(BsonDocument doc, IndexOptions options)
        {
            var val = doc.Get(this.Field).Normalize(options);

            return val.CompareTo(_value) <= (_equals ? 0 : -1);
        }
    }
}
