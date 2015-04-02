using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryIn : Query
    {
        private IEnumerable<BsonValue> _values;

        public QueryIn(string field, IEnumerable<BsonValue> values)
            : base(field)
        {
            _values = values;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            foreach (var value in _values.Distinct())
            {
                foreach (var node in Query.EQ(this.Field, value).ExecuteIndex(indexer, index))
                {
                    yield return node;
                }
            }
        }

        internal override void NormalizeValues(IndexOptions options)
        {
            var values = new List<BsonValue>();

            foreach (var value in _values.Distinct())
            {
                values.Add(value.Normalize(options));
            }

            _values = values;
        }

        internal override bool ExecuteFullScan(BsonDocument doc, IndexOptions options)
        {
            var val = doc.Get(this.Field).Normalize(options);

            foreach (var value in _values.Distinct())
            {
                var diff = val.CompareTo(value);

                if (diff == 0) return true;
            }

            return false;
        }
    }
}
