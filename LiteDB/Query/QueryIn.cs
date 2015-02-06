using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryIn : Query
    {
        public object[] Values { get; private set; }

        public QueryIn(string field, object[] values)
            : base(field)
        {
            this.Values = values;
        }

        internal override IEnumerable<IndexNode> Execute(LiteDatabase db, CollectionIndex index)
        {
            return db.Indexer.FindIn(index, this.Values);
        }
    }
}
