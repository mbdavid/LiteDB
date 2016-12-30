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
        private int _order;

        public QueryNot(Query query, int order)
            : base("_id")
        {
            _query = query;
            _order = order;
        }

        internal override void IndexFactory(Action<string, string> createIndex)
        {
            _query.IndexFactory(createIndex);
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            throw new NotSupportedException();
        }

        internal override IEnumerable<IndexNode> Run(CollectionPage col, IndexService indexer)
        {
            var result = _query.Run(col, indexer);
            var all = new QueryAll("_id", _order).Run(col, indexer);

            return all.Except(result, new IndexNodeComparer());
        }
    }
}