using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Contains query do not work with index, only full scan
    /// </summary>
    internal class QueryContains : Query
    {
        private BsonValue _value;

        public QueryContains(string field, BsonValue value)
            : base(field)
        {
            _value = value;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            return indexer
                .FindAll(index, Query.Ascending)
                .Where(x => x.Key.IsString && x.Key.AsString.Contains(_value));
        }

        internal override bool ExecuteDocument(BsonDocument doc)
        {
            var value = doc.Get(this.Field);

            return value.IsString ? value.AsString.Contains(_value.AsString) : false;
        }

        public override string ToString()
        {
            return string.Format("{0} contains {1}", this.Field, _value);
        }
    }
}