using System.Collections.Generic;

namespace LiteDB
{
    internal class QueryStartsWith : Query
    {
        private BsonValue _value;

        public QueryStartsWith(string field, BsonValue value)
            : base(field)
        {
            _value = value;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            // find first indexNode
            var value = _value.Normalize(index.Options);
            var node = indexer.Find(index, value, true, Query.Ascending);
            var str = value.AsString;

            // navigate using next[0] do next node - if less or equals returns
            while (node != null)
            {
                var valueString = node.Key.AsString;

                // value will not be null because null occurs before string (bsontype sort order)
                if (valueString.StartsWith(str))
                {
                    if (!node.DataBlock.IsEmpty)
                    {
                        yield return node;
                    }
                }
                else
                {
                    break; // if not more startswith, stop scanning
                }

                node = indexer.GetNode(node.Next[0]);
            }
        }
    }
}