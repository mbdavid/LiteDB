using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryMax : Query
    {
        public QueryMax(string field)
            : base(field)
        {
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            var last = indexer.GetNode(index.TailNode);
            var node = indexer.GetNode(last.Prev[0]);

            if(node.IsHeadTail) yield break;

            yield return node;
        }
    }
}
