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

        internal override IEnumerable<IndexNode> Run(CollectionPage col, IndexService indexer)
        {
            // run base query
            var result = _query.Run(col, indexer);

            this.UseIndex = _query.UseIndex;
            this.UseFilter = _query.UseFilter;

            if (_query.UseIndex)
            {
                // if is by index, resolve here
                var all = new QueryAll("_id", _order).Run(col, indexer);

                return all.Except(result, new IndexNodeComparer());
            }
            else
            {
                // if is by document, must return all nodes to be ExecuteDocument after
                return result;
            }
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            throw new NotSupportedException();
        }

        internal override bool FilterDocument(BsonDocument doc)
        {
            return !_query.FilterDocument(doc);
        }

        public override string ToString()
        {
            return string.Format("!({0})", _query);
        }
    }
}