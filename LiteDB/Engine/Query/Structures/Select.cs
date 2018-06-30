using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represent a Select expression
    /// </summary>
    internal class Select
    {
        private readonly BsonExpression _expression;
        private readonly bool _aggregate;

        public Select(BsonExpression expression, bool aggregate)
        {
            _expression = expression;
            _aggregate = aggregate;
        }

        public BsonExpression Expression => _expression;

        public bool Aggregate => _aggregate;
    }
}
