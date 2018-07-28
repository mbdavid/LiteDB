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
        public GroupBy(BsonExpression expression) : base(expression, 0)
        {
        }

        /// <summary>
        /// Get/Set transform expression in group by
        /// </summary>
        public BsonExpression Select { get; set; } = null;

        /// <summary>
        /// Get/Set if have filter after group by
        /// </summary>
        public BsonExpression Having { get; set; } = null;
    }
}
