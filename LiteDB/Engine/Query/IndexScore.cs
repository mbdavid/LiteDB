using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Calculate index score based on expression/collection index. 
    /// Score are any value between 0 (best) and 1 (worst)
    /// </summary>
    internal class IndexScore
    {
        public double Score { get; set; }
        public BsonExpression Expression { get; set; }

        private BsonExpression _value;
        private BsonExpressionType _type;
        private string _name;

        public IndexScore(CollectionIndex index, BsonExpression expr, BsonExpression value)
        {
            _name = index.Name;
            _type = expr.Type;
            _value = value;

            // copy root expression parameters to my value expression
            expr.Parameters.CopyTo(_value.Parameters);

            this.Expression = expr;

            // how unique is this index? (sometimes, unique key counter can be bigger than normal counter - it's because deleted nodes and will be fix only in next analyze collection)
            // 1 - Only unique values (best)
            // 0 - All nodes are same value (worst) - or not analyzed
            var u = (double)Math.Min(index.UniqueKeyCount, index.KeyCount) / (double)index.KeyCount;

            // determine a score multiplication
            // 1 for Equals (best - few (or single) results)
            // 0 for NotEquals (worst - (is basicly full scan)
            // .5 for interval (>, >=, <, <=, BETWEEN, STARTSWITH, ...)
            var m = 
                _type == BsonExpressionType.Equal ? 1d :
                _type == BsonExpressionType.NotEqual ? 0d : .5d;

            this.Score = u * m;
        }

        /// <summary>
        /// Create index query instance
        /// </summary>
        public Index CreateIndexQuery()
        {
            var value = _value.Execute(null).First();

            var index =
                _type == BsonExpressionType.Equal ? LiteDB.Index.EQ(_name, value) :
                _type == BsonExpressionType.LessThan ? LiteDB.Index.LT(_name, value) :
                _type == BsonExpressionType.LessThanOrEqual ? LiteDB.Index.LTE(_name, value) :
                _type == BsonExpressionType.GreaterThan ? LiteDB.Index.GT(_name, value) :
                _type == BsonExpressionType.GreaterThanOrEqual ? LiteDB.Index.GTE(_name, value) : null;

            return index;
        }
    }
}