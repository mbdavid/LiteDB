using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryAll : Query
    {
        public QueryAll()
            : base("_id")
        {
        }

        public QueryAll(string field)
            : base(field)
        {
        }

        internal override IEnumerable<IndexNode> Execute(LiteEngine engine, CollectionIndex index)
        {
            return engine.Indexer.FindAll(index);
        }
    }
}
