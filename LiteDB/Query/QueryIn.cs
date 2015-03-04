using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryIn : Query
    {
        public BsonArray Values { get; private set; }

        public QueryIn(string field, BsonArray values)
            : base(field)
        {
            this.Values = values;
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            foreach (var value in this.Values.Distinct())
            {
                foreach (var node in indexer.FindEquals(index, value))
                {
                    yield return node;
                }
            }
        }
    }
}
