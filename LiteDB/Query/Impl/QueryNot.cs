using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryNot : Query
    {
        private BsonValue _value;
        private int _order;

        public QueryNot(string field, BsonValue value, int order)
            : base(field)
        {
            _value = value;
            _order = order;
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            var value = _value.Normalize(index.Options);

            return indexer.FindAll(index, _order).Where(x => x.Value.CompareTo(value) != 0);
        }
    }
}
