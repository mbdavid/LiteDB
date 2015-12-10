using System.Collections.Generic;
using System.Linq;

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
    }
}