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
        /// Get index cost (lower is best)
        /// </summary>
        public long IndexCost { get; internal set; } = 0; // not calculated

        #region Explain Plan

        /// <summary>
        /// Get detail plan engine will execute
        /// </summary>
        public string GetExplainPlan()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Cost: " + this.IndexCost);
            sb.AppendLine("Index: " + this.Index.ToString() + " " + (this.Index.Order == Query.Ascending ? "ASC" : "DESC"));

            if (this.Filters.Count > 0)
            {
                sb.AppendLine("Filters: " + string.Join(" AND ", this.Filters.Select(x => $"({x.Source})")));
            }

            if (this.Select != null)
            {
                sb.AppendLine("Select: " + this.Select.Source);
            }

            if (this.OrderBy != null)
            {
                sb.AppendLine("OrderBy: " + this.OrderBy.Source + (this.Order == Query.Ascending ? " ASC" : " DESC"));
            }

            if (this.Limit != int.MaxValue)
            {
                sb.AppendLine("Limit: " + this.Limit);
            }

            if (this.Offset > 0)
            {
                sb.AppendLine("Offset: " + this.Offset);
            }

            return sb.ToString();
        }

        #endregion
    }
}