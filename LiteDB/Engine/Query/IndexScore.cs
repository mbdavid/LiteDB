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
        public double Score { get; private set; }
        public BsonExpression Expression { get; private set; }
        public Index Index { get; private set; }

        public IndexScore(CollectionIndex index, BsonExpression expr, BsonExpression value)
        {
            // copy root expression parameters to my value expression
            expr.Parameters.CopyTo(value.Parameters);

            this.Expression = expr;

            // create index instances
            var indexes = value.Execute(null).Select(x => this.CreateIndex(expr.Type, index.Name, x)).ToList();

            this.Index = indexes.Count == 1 ? indexes[0] : new IndexOr(indexes);

            // calcs index score
            this.Score = this.Index.GetScore(index);
        }

        /// <summary>
        /// Create index based on expression conditional
        /// </summary>
        private Index CreateIndex(BsonExpressionType type, string name, BsonValue value)
        {
            return
                type == BsonExpressionType.Equal ? LiteDB.Index.EQ(name, value) :
                type == BsonExpressionType.Between ? LiteDB.Index.Between(name, value.AsArray[0], value.AsArray[1]) :
                type == BsonExpressionType.Like ? LiteDB.Index.Like(name, value.AsString) :
                type == BsonExpressionType.GreaterThan ? LiteDB.Index.GT(name, value) :
                type == BsonExpressionType.GreaterThanOrEqual ? LiteDB.Index.GTE(name, value) :
                type == BsonExpressionType.LessThan ? LiteDB.Index.LT(name, value) :
                type == BsonExpressionType.LessThanOrEqual ? LiteDB.Index.LTE(name, value) :
                type == BsonExpressionType.NotEqual ? LiteDB.Index.Not(name, value) : null;
        }
    }
}