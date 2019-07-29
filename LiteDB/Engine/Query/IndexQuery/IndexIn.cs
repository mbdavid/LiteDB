using System;
using System.Collections.Generic;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement IN index operation. Value must be an array
    /// </summary>
    internal class IndexIn : Index
    {
        private BsonArray _values;

        public IndexIn(string name, BsonArray values, int order)
            : base(name, order)
        {
            _values = values;
        }

        public override uint GetCost(CollectionIndex index)
        {
            var count = (uint)_values.Count;

            if (index.Unique)
            {
                return count; // best case, ever!
            }
            else if (index.KeyCount == 0)
            {
                return uint.MaxValue;
            }
            else
            {
                // use same cost from Equals, but multiply with values count
                var density = index.Density;

                var cost = density == 0 ? index.KeyCount : (uint)(1d / density);

                return cost * count;
            }
        }

        public override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            foreach (var value in _values.Distinct())
            {
                foreach (var node in Index.EQ(this.Name, value).Execute(indexer, index))
                {
                    yield return node;
                }
            }
        }

        public override string ToString()
        {
            return string.Format("INDEX SEEK({0} IN {1})", this.Name, JsonSerializer.Serialize(_values));
        }
    }
}