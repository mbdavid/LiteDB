using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryLess : Query
    {
        public object Value { get; private set; }
        public bool OrEquals { get; private set; }

        public QueryLess(string field, object value, bool orEquals)
            : base(field)
        {
            this.Value = value;
            this.OrEquals = orEquals;
        }

        internal override IEnumerable<IndexNode> Execute(LiteEngine engine, CollectionIndex index)
        {
            return engine.Indexer.FindLessThan(index, this.Value, this.OrEquals);
        }
    }
}
