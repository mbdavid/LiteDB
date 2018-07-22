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
        private readonly bool _all;

        public Select(BsonExpression expression, bool all)
        {
            _expression = expression;
            _all = all;
        }

        public BsonExpression Expression => _expression;

        public bool All => _all;
    }
}
