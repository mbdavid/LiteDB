using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryStartsWith : Query
    {
        public BsonValue Value { get; private set; }

        public QueryStartsWith(string field, BsonValue value)
            : base(field)
        {
            this.Value = value;
        }

        internal override IEnumerable<IndexNode> Execute(LiteDatabase db, CollectionIndex index)
        {
            return db.Indexer.FindStarstWith(index, this.Value);
        }
    }
}
