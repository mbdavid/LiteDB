using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Execute an index scan passing a Func as where
    /// </summary>
    internal class QueryWhere : Query
    {
        private Func<BsonValue, bool> _func;
        private int _order;

        public QueryWhere(string field, Func<BsonValue, bool> func, int order)
            : base(field)
        {
            _func = func;
            _order = order;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            return indexer
                .FindAll(index, _order)
                .Where(i => _func(i.Key));
        }
    }
}