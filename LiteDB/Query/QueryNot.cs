using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryNot : Query
    {
        public object Value { get; private set; }

        public QueryNot(string field, object value)
            : base(field)
        {
            this.Value = value;
        }

        internal override IEnumerable<IndexNode> Execute(LiteEngine engine, CollectionIndex index)
        {
            return engine.Indexer.FindAll(index).Where(x => x.Key.CompareTo(new IndexKey(this.Value)) != 0);
        }
    }
}
