using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryAll : Query
    {
        private int _order;

        public QueryAll(string field, int order)
            : base(field)
        {
            _order = order;
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            return indexer.FindAll(index, _order);
        }
    }
}
