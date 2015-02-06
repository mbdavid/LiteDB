using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryStartsWith : Query
    {
        public string Value { get; private set; }

        public QueryStartsWith(string field, string value)
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
