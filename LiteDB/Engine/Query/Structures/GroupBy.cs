using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represent an GroupBy definition (is based on OrderByDefinition)
    /// </summary>
    internal class GroupBy : OrderBy
    {
        private readonly BsonExpression _select;
        private readonly BsonExpression _having;

        public GroupBy(BsonExpression expression, BsonExpression select, BsonExpression having) : base(expression, 0)
        {
            _select = select;
            _having = having;
        }

        /// <summary>
        /// Get/Set transform expression in group by
        /// </summary>
        public BsonExpression Select => _select;

        /// <summary>
        /// Get/Set if have filter after group by
        /// </summary>
        public BsonExpression Having => _having;
    }
}
