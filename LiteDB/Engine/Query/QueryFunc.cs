using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal class QueryFunc : Query
    {
        private Func<BsonValue, bool> _func;

        public QueryFunc(string field, Func<BsonValue, bool> func)
            : base(field)
        {
            _func = func;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            return indexer
                .FindAll(index, Query.Ascending)
                .Where(i => _func(i.Key));
        }
    }
}