using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Not is an Index Scan operation
    /// </summary>
    internal class QueryNot : Query
    {
        private BsonValue _value;

        public QueryNot(string field, BsonValue value)
            : base(field)
        {
            _value = value;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            var value = _value.Normalize(index.Options);

            return indexer.FindAll(index, Query.Ascending).Where(x => x.Key.CompareTo(value) != 0);
        }

        internal override void NormalizeValues(IndexOptions options)
        {
            _value = _value.Normalize(options);
        }

        internal override bool ExecuteFullScan(BsonDocument doc, IndexOptions options)
        {
            var val = doc.Get(this.Field).Normalize(options);

            return val.CompareTo(_value) != 0;
        }
    }
}
