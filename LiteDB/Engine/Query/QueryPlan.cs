using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB.Engine
{
    /// <summary>
    /// This class are result from optimization from QueryBuild in QueryAnalyzer. Indicate how engine must run query - there is no more decisions to engine made, must only execute as query was defined
    /// Contains used index and estimate cost to run
    /// </summary>
    internal class QueryPlan
    {
        public QueryPlan(string collection)
        {
            this.Collection = collection;
        }

        /// <summary>
        /// Get collection name (required)
        /// </summary>
        public string Collection { get; set; } = null;

        /// <summary>
        /// Index used on query (required)
        /// </summary>
        public Index Index { get; set; } = null;

        /// <summary>
        /// If true, select expressoin must run over all resultset. Otherwise, each document result will be transformed by select expression
        /// </summary>
        public bool Aggregate { get; set; } = false;

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
        /// Expression to group by document results
        /// </summary>
        public BsonExpression GroupBy { get; set; } = null;

        /// <summary>
        /// Define group by order
        /// </summary>
        public int GroupByOrder { get; set; } = Query.Ascending;

        /// <summary>
        /// If contains group by expression, must be run OrderBy over GroupBy or already sorted?
        /// </summary>
        public bool RunOrderByOverGroupBy { get; set; } = true;

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

        /// <summary>
        /// Get this this plan run over system collection
        /// </summary>
        public bool IsVirtual { get; set; } = false;

        #region Explain Plan

        /// <summary>
        /// Get detail plan engine will execute
        /// </summary>
        public string GetExplainPlan()
        {
            var sb = new StringBuilder();

            sb.AppendLine("collection: " + this.Collection);
            sb.AppendLine("index: " + this.Index.ToString() + " " + (this.Index.Order == Query.Ascending ? "ASC" : "DESC"));
            sb.AppendLine("cost: " + this.IndexCost);

            if (this.Filters.Count > 0)
            {
                foreach(var filter in this.Filters)
                {
                    sb.AppendLine("filter: " + this.GetExpression(filter));
                }
            }

            if (this.Select != null)
            {
                sb.AppendLine("select: " + this.GetExpression(this.Select));
            }

            if (this.GroupBy != null)
            {
                sb.AppendLine("groupBy: " + this.GetExpression(this.GroupBy) + (this.GroupByOrder == Query.Ascending ? " ASC" : " DESC"));
            }

            if (this.OrderBy != null)
            {
                sb.AppendLine("orderBy: " + this.GetExpression(this.OrderBy) + (this.Order == Query.Ascending ? " ASC" : " DESC"));
            }

            if (this.Limit != int.MaxValue)
            {
                sb.AppendLine("limit: " + this.Limit);
            }

            if (this.Offset > 0)
            {
                sb.AppendLine("offset: " + this.Offset);
            }

            if (this.KeyOnly)
            {
                sb.AppendLine("keyOnly: true");
            }

            if (this.Aggregate)
            {
                sb.AppendLine("aggregate: true");
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Render an expression as string - add parameter if have any
        /// </summary>
        private string GetExpression(BsonExpression expr)
        {
            return expr.Source +
                (expr.Parameters.Count > 0 ?
                " >> @" + JsonSerializer.Serialize(expr.Parameters) :
                "");
        }

        #endregion
    }
}