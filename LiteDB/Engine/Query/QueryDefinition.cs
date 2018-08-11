using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    public class QueryDefinition
    {
        public QueryDefinition()
        {
        }

        public List<BsonExpression> Where { get; set; } = new List<BsonExpression>();
        public List<BsonExpression> Includes { get; set; } = new List<BsonExpression>();

        public BsonExpression OrderBy { get; set; } = null;
        public int Order { get; set; } = Query.Ascending;

        public BsonExpression GroupBy { get; set; } = null;
        public BsonExpression Having { get; set; } = null;

        public BsonExpression Select { get; set; } = null;
        public bool SelectAll { get; set; } = false;

        public int Offset { get; set; } = 0;
        public int Limit { get; set; } = int.MaxValue;
        public bool ForUpdate { get; set; } = false;

        public string Into { get; set; }
        public BsonAutoId IntoAutoId { get; set; } = BsonAutoId.ObjectId;

        public bool ExplainPlan { get; set; }
    }
}
