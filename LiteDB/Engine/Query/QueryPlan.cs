using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// This class are result from optimization from QueryBuild in QueryAnalyzer. Indicate how engine must run query - there is no more decisions to engine made, must only execute as query was defined
    /// Contains used index and estimate cost to run
    /// </summary>
    public class QueryPlan
    {
        /// <summary>
        /// Index used on query (required)
        /// </summary>
        public Index Index { get; set; } = null;

        /// <summary>
        /// If true, gereate document result only with IndexNode.Key (avoid load all document)
        /// </summary>
        public bool KeyOnly { get; set; } = false;

        /// <summary>
        /// Indicate this query is for update (lock mode = Write)
        /// </summary>
        public bool ForUpdate { get; set; } = false;

        /// <summary>
        /// List of filters of documents
        /// </summary>
        public List<BsonExpression> Filters { get; set; } = new List<BsonExpression>();

        /// <summary>
        /// List of includes must be done BEFORE filter (it's not optimized but some filter will use this include)
        /// </summary>
        public List<BsonExpression> IncludeBefore { get; set; } = new List<BsonExpression>();

        /// <summary>
        /// List of includes must be done AFTER filter (it's optimized because will include result only)
        /// </summary>
        public List<BsonExpression> IncludeAfter { get; set; } = new List<BsonExpression>();

        /// <summary>
        /// Expression to order by resultset
        /// </summary>
        public BsonExpression OrderBy { get; set; } = null;

        /// <summary>
        /// Order used in OrderBy expression
        /// </summary>
        public int Order { get; set; } = Query.Ascending;

        /// <summary>
        /// Expression to group by result generation a new documents { key: [GroupByExpress], values: [Documents] }
        /// </summary>
        public BsonExpression GroupBy { get; set; } = null;

        /// <summary>
        /// Limit resultset
        /// </summary>
        public int Limit { get; set; } = int.MaxValue;

        /// <summary>
        /// Skip documents before returns
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Transaformation data before return - if null there is no transform (return document)
        /// </summary>
        public BsonExpression Select { get; set; } = null;

        /// <summary>
        /// Get index score from 0 (worst - full scan) to 1 (best - equals unique key)
        /// </summary>
        public double IndexScore { get; set; } = -1; // not calculated

        /// <summary>
        /// Get explain plan engine will execute
        /// </summary>
        internal string ExplainPlan
        {
            get
            {
                return
                    "Index Score: " + this.IndexScore + Environment.NewLine +
                    "Index: " + this.Index.ToString() + Environment.NewLine +
                    "Filters: " + string.Join(" AND ", this.Filters.Select(x => $"({x.Source})")) + Environment.NewLine +
                    "OrderBy: " + (this.OrderBy?.Source ?? "-none-") + " " + this.Order;
            }
        }
    }
}