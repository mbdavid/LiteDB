using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryNot : Query
    {
        public BsonValue Value { get; private set; }

        public QueryNot(string field, BsonValue value)
            : base(field)
        {
            this.Value = value;
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            return indexer.FindAll(index).Where(x => x.Value.CompareTo(this.Value) != 0);
        }
    }
}
