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

        internal override bool FilterDocument(BsonDocument doc)
        {
            var value = doc.Get(this.Field);

            return value.IsString ? value.AsString.Contains(_value.AsString) : false;
        }

        public override string ToString()
        {
            return string.Format("{0}([{1}] contains {2})",
                this.UseFilter ? "F" : this.UseIndex ? "I" : "",
                this.Field,
                _value);
        }
    }
}