using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryBetween : Query
    {
        public object Start { get; private set; }
        public object End { get; private set; }

        public QueryBetween(string field, object start, object end)
            : base(field)
        {
            this.Start = start;
            this.End = end;
        }

        internal override IEnumerable<IndexNode> Execute(LiteEngine engine, CollectionIndex index)
        {
            return engine.Indexer.FindBetween(index, this.Start, this.End);
        }
    }
}
