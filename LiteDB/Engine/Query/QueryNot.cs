using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Not is an Index Scan operation
    /// </summary>
    internal class QueryNot : Query
    {
        private Query _query;

        public QueryNot(Query query)
            : base("_id")
        {
            _query = query;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            var result = _query.ExecuteIndex(indexer, index);

            return indexer.FindAll(index, Query.Ascending)
                .Except(result, new IndexNodeComparer());
        }
    }
}