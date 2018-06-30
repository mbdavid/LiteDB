using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represent an OrderBy definition
    /// </summary>
    internal class OrderBy
    {
        private readonly BsonExpression _expression;

        public OrderBy(BsonExpression expression, int order)
        {
            _expression = expression;
            this.Order = order;
        }

        public BsonExpression Expression => _expression;

        public int Order { get; set; }
    }
}
