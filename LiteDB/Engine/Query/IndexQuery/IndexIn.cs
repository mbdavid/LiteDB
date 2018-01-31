using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Implement index in operation (or values)
    /// </summary>
    internal class IndexIn : Index
    {
        private IEnumerable<BsonValue> _values;

        public IndexIn(string name, IEnumerable<BsonValue> values)
            : base(name)
        {
            _values = values;
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
    }
}