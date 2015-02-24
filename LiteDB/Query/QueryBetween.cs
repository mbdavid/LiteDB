using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryBetween : Query
    {
        public BsonValue Start { get; private set; }
        public BsonValue End { get; private set; }

        public QueryBetween(string field, BsonValue start, BsonValue end)
            : base(field)
        {
            this.Start = start;
            this.End = end;
        }

        internal override IEnumerable<IndexNode> Execute(LiteDatabase db, CollectionIndex index)
        {
            return db.Indexer.FindBetween(index, this.Start, this.End);
        }
    }
}
