using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class QueryMin : Query
    {
        public QueryMin(string field)
            : base(field)
        {
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            var first = indexer.GetNode(index.HeadNode);
            var node = indexer.GetNode(first.Next[0]);

            if (node.IsHeadTail) yield break;

            yield return node;
        }
    }
}
