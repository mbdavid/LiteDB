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

            double m = 0;

            // determine a score multiplication by operation (from best=1 to worst=0)
            switch (_type)
            {
                case BsonExpressionType.Equal:
                //case BsonExpressionType.In:
                    m = 1; break;
                //case BsonExpressionType.Between:
                //case BsonExpressionType.StartsWith:
                //    m = 0.1; break;
                case BsonExpressionType.GreaterThan:
                case BsonExpressionType.GreaterThanOrEqual:
                case BsonExpressionType.LessThan:
                case BsonExpressionType.LessThanOrEqual:
                    m = 0.01; break;
                default:
                    m = 0.0001;
                    break;
            }

            // calcs index score
            this.Score = u * m;
        }

        /// <summary>
        /// Create index query instance
        /// </summary>
        public Index CreateIndexQuery()
        {
            var indexes = _value.Execute(null).Select(x => this.GetIndex(x)).ToList();

            if (indexes.Count == 1)
            {
                return indexes[0];
            }
            else
            {
                return new IndexOr(indexes);
            }
        }

        private Index GetIndex(BsonValue value)
        {
            return
                _type == BsonExpressionType.Equal ? LiteDB.Index.EQ(_name, value) :
                _type == BsonExpressionType.Between ? LiteDB.Index.Between(_name, value.AsArray[0], value.AsArray[1]) :
                _type == BsonExpressionType.StartsWith ? LiteDB.Index.StartsWith(_name, value.AsString) :
                _type == BsonExpressionType.GreaterThan ? LiteDB.Index.GT(_name, value) :
                _type == BsonExpressionType.GreaterThanOrEqual ? LiteDB.Index.GTE(_name, value) :
                _type == BsonExpressionType.LessThan ? LiteDB.Index.LT(_name, value) :
                _type == BsonExpressionType.LessThanOrEqual ? LiteDB.Index.LTE(_name, value) :
                _type == BsonExpressionType.Contains ? LiteDB.Index.Contains(_name, value) :
                _type == BsonExpressionType.EndsWith ? LiteDB.Index.EndsWith(_name, value) :
                _type == BsonExpressionType.NotEqual ? LiteDB.Index.Not(_name, value) : null;
        }
    }
}