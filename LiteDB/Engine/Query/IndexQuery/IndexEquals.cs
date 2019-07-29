using System;
using System.Collections.Generic;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
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

        public override uint GetCost(CollectionIndex index)
        {
            if (index.Unique)
            {
                return 1; // best case, ever!
            }
            else if(index.KeyCount == 0)
            {
                return uint.MaxValue; // index are not analyzed
            }
            else
            {
                var density = index.Density;

                var cost = density == 0 ? index.KeyCount : (uint)Math.Round(1d / density);

                return cost;
            }
        }

        public override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            var node = indexer.Find(index, _value, false, Query.Ascending);

            if (node == null) yield break;

            yield return node;

            if (index.Unique == false)
            {
                // navigate in both sides to return all nodes found
                var first = node;

                // first go fordward
                while (!node.Next[0].IsEmpty && ((node = indexer.GetNode(node.Next[0])).Key.CompareTo(_value) == 0))
                {
                    if (node.Key.IsMinValue || node.Key.IsMaxValue) break;

                    yield return node;
                }

                node = first;
                
                // and than, go backward
                while (!node.Prev[0].IsEmpty && ((node = indexer.GetNode(node.Prev[0])).Key.CompareTo(_value) == 0))
                {
                    if (node.Key.IsMinValue || node.Key.IsMaxValue) break;

                    yield return node;
                }
            }
        }

        public override string ToString()
        {
            return string.Format("INDEX SEEK({0} = {1})", this.Name, _value);
        }
    }
}