using System;
using System.Collections.Generic;
using System.Linq;

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

        internal override bool FilterDocument(BsonDocument doc)
        {
            var val = doc.Get(this.Field);

            foreach (var value in _values.Distinct())
            {
                var diff = val.CompareTo(value);

                if (diff == 0) return true;
            }

            return false;
        }

        public override string ToString()
        {
            return string.Format("{0}([{1}] in {2})",
                this.UseFilter ? "F" : this.UseIndex ? "I" : "",
                this.Field,
                _values);
        }
    }
}