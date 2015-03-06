using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryLess : Query
    {
        private BsonValue _value;
        private bool _equals;
        private int _order;

        public QueryLess(string field, BsonValue value, bool equals, int order)
            : base(field)
        {
            _value = value;
            _equals = equals;
            _order = order;
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            throw new NotImplementedException();
        }
    }
}
