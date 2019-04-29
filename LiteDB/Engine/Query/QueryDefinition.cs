using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    public class QueryDefinition
    {
        public List<BsonExpression> Where { get; set; } = new List<BsonExpression>();
        public List<BsonExpression> Includes { get; set; } = new List<BsonExpression>();

        public BsonExpression OrderBy { get; set; } = null;
        public int Order { get; set; } = Query.Ascending;

        public BsonExpression GroupBy { get; set; } = null;
        public BsonExpression Having { get; set; } = null;

        public BsonExpression Select { get; set; } = null;

        public int Offset { get; set; } = 0;
        public int Limit { get; set; } = int.MaxValue;
        public bool ForUpdate { get; set; } = false;

        public string Into { get; set; }
        public BsonAutoId IntoAutoId { get; set; } = BsonAutoId.ObjectId;

        public bool ExplainPlan { get; set; }

        /// <summary>
        /// Check some rules if query contains concise rules
        /// </summary>
        internal void Validate()
        {
            if (this.Having != null && this.GroupBy == null)
            {
                throw new LiteException(0, "HAVING require GROUP BY expression");
            }
            if (this.GroupBy != null && this.OrderBy != null)
            {
                throw new LiteException(0, "GROUP BY has no support for ORDER BY");
            }
            if (this.GroupBy != null && this.GroupBy.IsScalar == false)
            {
                throw new LiteException(0, "GROUP BY expression must be a scalar expression");
            }
            if (this.OrderBy != null && this.OrderBy.IsScalar == false)
            {
                throw new LiteException(0, "ORDER BY expression must be a scalar expression");
            }
        }
    }
}
