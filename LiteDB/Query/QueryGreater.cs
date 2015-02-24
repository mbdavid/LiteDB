using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryGreater : Query
    {
        public BsonValue Value { get; private set; }
        public bool OrEquals { get; private set; }

        public QueryGreater(string field, BsonValue value, bool orEquals)
            : base(field)
        {
            this.Value = value;
            this.OrEquals = orEquals;
        }

        internal override IEnumerable<IndexNode> Execute(LiteDatabase db, CollectionIndex index)
        {
            return db.Indexer.FindGreaterThan(index, this.Value, this.OrEquals);
        }
    }
}
