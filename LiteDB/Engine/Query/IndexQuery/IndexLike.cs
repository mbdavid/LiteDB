using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal class IndexLike : Index
    {
        private string _startsWith;
        private bool _testSqlLike;
        private string _pattern;

        public IndexLike(string name, BsonValue value, int order)
            : base(name, order)
        {
            _pattern = value.AsString;
            _startsWith = _pattern.SqlLikeStartsWith(out _testSqlLike);
        }

        internal override uint GetCost(CollectionIndex index)
        {
            if (_startsWith.Length > 0)
            {
                // need some statistics here... assuming read 20% of total
                return (uint)(index.KeyCount * (0.2));
            }
            else
            {
                return index.KeyCount;
            }
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            // if contains startsWith string, search using index Find
            // otherwise, use index full scan and test results
            return _startsWith.Length == 0 ? 
                this.ExecuteStartsWith(indexer, index) : 
                this.ExecuteLike(indexer, index);
        }

        private IEnumerable<IndexNode> ExecuteStartsWith(IndexService indexer, CollectionIndex index)
        {
            // find first indexNode
            var node = indexer.Find(index, _startsWith, true, this.Order);

            while (node != null)
            {
                var valueString = node.Key.AsString;

                // value will not be null because null occurs before string (bsontype sort order)
                if (valueString.StartsWith(_startsWith))
                {
                    // must still testing SqlLike method for rest of pattern - only if exists more to test (avoid slow SqlLike test)
                    if (!node.DataBlock.IsEmpty && (_testSqlLike && valueString.SqlLike(_pattern)))
                    {
                        yield return node;
                    }
                }
                else
                {
                    break; // if no more starts with, stop scanning
                }

                node = indexer.GetNode(node.NextPrev(0, this.Order));
            }
        }

        private IEnumerable<IndexNode> ExecuteLike(IndexService indexer, CollectionIndex index)
        {
            return indexer
                .FindAll(index, this.Order)
                .Where(x => x.Key.IsString && x.Key.AsString.SqlLike(_pattern));
        }

        public override string ToString()
        {
            return string.Format("{0}({1})",
                _startsWith.Length > 0 ? "INDEX RANGE SCAN" : "FULL INDEX SCAN",
                this.Name);
        }
    }
}