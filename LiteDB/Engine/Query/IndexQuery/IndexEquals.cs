using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Implement equals index operation =
    /// </summary>
    internal class IndexEquals : Index
    {
        private BsonValue _value;

        public IndexEquals(string name, BsonValue value)
            : base(name, Query.Ascending)
        {
            _value = value;
        }

        internal override uint GetCost(CollectionIndex index)
        {
            if (index.Unique)
            {
                return 1; // best case, ever!
            }
            else
            {
                // how unique is this index? (sometimes, unique key counter can be bigger than normal counter - it's because deleted nodes and will be fix only in next analyze collection)
                var uniq = (double)Math.Min(index.UniqueKeyCount, index.KeyCount);

                return (uint)(index.KeyCount / uniq);
            }
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            var node = indexer.Find(index, _value, false, Query.Ascending);

            if (node == null) yield break;

            yield return node;

            if (index.Unique == false)
            {
                // navigate using next[0] do next node - if equals, returns
                while (!node.Next[0].IsEmpty && ((node = indexer.GetNode(node.Next[0])).Key.CompareTo(_value) == 0))
                {
                    if (node.IsHeadTail(index)) yield break;

                    yield return node;
                }
            }
        }

        public override string ToString()
        {
            return string.Format("INDEX SCAN({0})", this.Name);
        }
    }
}