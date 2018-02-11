using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal class QueryStringEquals : Query
    {
        private BsonValue _value;
        private bool _ignoreCase;
        private StringComparison _strComp => _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        public QueryStringEquals(string field, string value, bool ignoreCase)
            : base(field)
        {
            _value = value;
            _ignoreCase = ignoreCase;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            return indexer
                .FindAll(index, Query.Ascending)
                .Where(x => x.Key.IsString && x.Key.AsString.Equals(_value, _strComp));
        }

        internal override bool FilterDocument(BsonDocument doc)
            => Expression.Execute(doc, true).Any(x => string.Equals(_value.AsString, x.AsString, _strComp));

        public override string ToString()
            => $"{(UseFilter ? "Filter" : UseIndex ? "Seek" : "")}({Expression?.ToString() ?? Field} = {_value})";
    }
}
