using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal class QueryStringEquals : Query
    {
        private BsonValue _value;
        private StringComparison _comparison;

        public QueryStringEquals(string field, string value, StringComparison comparison)
            : base(field)
        {
            _value = value;
            _comparison = comparison;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            return indexer
                .FindAll(index, Query.Ascending)
                .Where(x => x.Key.IsString && x.Key.AsString.Equals(_value, _comparison));
        }

        internal override bool FilterDocument(BsonDocument doc)
            => Expression.Execute(doc, true).Any(x => string.Equals(_value.AsString, x.AsString, _comparison));

        public override string ToString()
            => $"{(UseFilter ? "Filter" : UseIndex ? "Seek" : "")}({Expression?.ToString() ?? Field} = {_value})";
    }
}
