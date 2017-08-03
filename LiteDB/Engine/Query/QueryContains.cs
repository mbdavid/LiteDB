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
        private StringComparison? _options = null;

        public QueryContains(string field, BsonValue value)
            : base(field)
        {
            _value = value;
        }

        public QueryContains(string field, BsonValue value, StringComparison options) : this(field, value)
        {
            _options = options;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            if (_options.HasValue)
                return indexer
                .FindAll(index, Query.Ascending)
                .Where(x => x.Key.IsString && x.Key.AsString.Contains(_value, _options.Value));

            return indexer
                .FindAll(index, Query.Ascending)
                .Where(x => x.Key.IsString && x.Key.AsString.Contains(_value));
        }
    }
}