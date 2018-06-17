using System;
using System.Collections.Generic;
using System.Linq;

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
            return string.Format("INDEX SCAN({0} IN {1})", this.Name, JsonSerializer.Serialize(_values));
        }
    }
}