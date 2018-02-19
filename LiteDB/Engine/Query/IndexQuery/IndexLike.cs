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

        internal override double GetScore(CollectionIndex index)
        {
            if (_startsWith.Length > 0)
            {
                // how unique is this index? (sometimes, unique key counter can be bigger than normal counter - it's because deleted nodes and will be fix only in next analyze collection)
                // 1 - Only unique values (best)
                // 0 - All nodes are same value (worst) - or not analyzed
                var u = (double)Math.Min(index.UniqueKeyCount, index.KeyCount) / (double)index.KeyCount;

                return u;
            }
            else
            {
                // index full scan
                return 0;
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
            return string.Format("LIKE({0}) {1}", this.Name, this.Order == Query.Ascending ? "ASC" : "DESC");
        }
    }
}