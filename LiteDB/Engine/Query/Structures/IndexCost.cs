using System;
using System.Collections.Generic;
using System.Linq;
using Idx = LiteDB.Engine.Index;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Calculate index cost based on expression/collection index. 
    /// Lower cost is better - lowest will be selected
    /// </summary>
    internal class IndexCost
    {
        public uint Cost { get; }

        /// <summary>
        /// Get filtered expression: "$._id = 10"
        /// </summary>
        public BsonExpression Expression { get; }

        /// <summary>
        /// Get index expression only: "$._id"
        /// </summary>
        public string IndexExpression { get; }

        /// <summary>
        /// Get created Index instance used on query
        /// </summary>
        public Index Index { get; }

        public IndexCost(CollectionIndex index, BsonExpression expr, BsonExpression value)
        {
            // copy root expression parameters to my value expression
            expr.Parameters.CopyTo(value.Parameters);

            this.IndexExpression = index.Expression;
            this.Expression = expr;

            // create index instance
            this.Index = value.Execute().Select(x => this.CreateIndex(expr.Type, index.Name, x)).FirstOrDefault();

            ENSURE(this.Index != null, "index must be not null");

            // calcs index cost
            this.Cost = this.Index.GetCost(index);
        }

        // used when full index search
        public IndexCost(CollectionIndex index)
        {
            this.Expression = BsonExpression.Create(index.Expression);
            this.Index = Index.All(index.Name);
            this.Cost = this.Index.GetCost(index);
            this.IndexExpression = index.Expression;
        }

        /// <summary>
        /// Create index based on expression predicate
        /// </summary>
        private Index CreateIndex(BsonExpressionType type, string name, BsonValue value)
        {
            return
                type == BsonExpressionType.Equal ? Idx.EQ(name, value) :
                type == BsonExpressionType.Between ? Idx.Between(name, value.AsArray[0], value.AsArray[1]) :
                type == BsonExpressionType.Like ? Idx.Like(name, value.AsString) :
                type == BsonExpressionType.GreaterThan ? Idx.GT(name, value) :
                type == BsonExpressionType.GreaterThanOrEqual ? Idx.GTE(name, value) :
                type == BsonExpressionType.LessThan ? Idx.LT(name, value) :
                type == BsonExpressionType.LessThanOrEqual ? Idx.LTE(name, value) :
                type == BsonExpressionType.NotEqual ? Idx.Not(name, value) :
                type == BsonExpressionType.In ? (value.IsArray ? Idx.In(name, value.AsArray) : Idx.EQ(name, value)) : 
                null;
        }
    }
}