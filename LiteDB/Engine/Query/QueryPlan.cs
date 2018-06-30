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
        /// Index expression that will be used in index (source only)
        /// </summary>
        public string IndexExpression { get; set; } = null;

        /// <summary>
        /// If true, select expressoin must run over all resultset. Otherwise, each document result will be transformed by select expression
        /// </summary>
        public bool Aggregate { get; set; } = false;

        /// <summary>
        /// If true, gereate document result only with IndexNode.Key (avoid load all document)
        /// </summary>
        public bool IsIndexKeyOnly { get; set; } = false;

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
        /// Filter expression running over GroupBy result
        /// </summary>
        public BsonExpression Having { get; set; } = null;

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
        public BsonExpression Select { get; set; }

        /// <summary>
        /// Get fields name that will be deserialize from disk
        /// </summary>
        public HashSet<string> Fields { get; set; }

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
        public BsonDocument GetExplainPlan()
        {
            var doc = new BsonDocument
            {
                ["collection"] = this.Collection,
                ["snaphost"] = this.ForUpdate ? "write" : "read",
                ["index"] = new BsonDocument
                {
                    ["name"] = this.Index.Name,
                    ["mode"] = this.Index.ToString(),
                    ["expr"] = this.IndexExpression,
                    ["order"] = this.Index.Order,
                    ["cost"] = (int)this.IndexCost
                },
                ["load"] = new BsonDocument
                {
                    ["loader"] = this.IsVirtual ? "virtual" : (this.IsIndexKeyOnly ? "index" : "document"),
                    ["fields"] = new BsonArray(this.Fields.Select(x => new BsonValue(x))),
                },
                ["pipe"] = this.GroupBy == null ? "queryPipe" : "groupByPipe",
                ["includeBefore"] = this.IncludeBefore.Count == 0 ?
                    BsonValue.Null :
                    new BsonArray(this.IncludeBefore.Select(x => new BsonValue(x.Source))),
                ["filters"] = this.Filters.Count == 0 ?
                    BsonValue.Null :
                    new BsonArray(this.Filters.Select(x => new BsonValue(x.Source))),
                ["includeAfter"] = this.IncludeAfter.Count == 0 ?
                    BsonValue.Null :
                    new BsonArray(this.IncludeBefore.Select(x => new BsonValue(x.Source))),
                ["groupBy"] = this.GroupBy == null ?
                    BsonValue.Null :
                    new BsonDocument
                    {
                        ["expr"] = this.GroupBy.Source,
                        ["order"] = this.GroupByOrder,
                        ["select"] = this.Select.Source,
                        ["having"] = this.Having?.Source,
                    },
                ["orderBy"] = this.OrderBy == null ?
                    BsonValue.Null :
                    new BsonDocument
                    {
                        ["expr"] = this.OrderBy?.Source,
                        ["order"] = this.Order,
                    },
                ["limit"] = this.Limit,
                ["offset"] = this.Offset,
                ["select"] = this.GroupBy != null ?
                    BsonValue.Null :
                    new BsonDocument
                    {
                        ["expr"] = this.Select.Source,
                        ["aggregate"] = this.Aggregate
                    }
            };

            return doc;
        }

        /// <summary>
        /// Render an expression as string - add parameter if have any
        /// </summary>
        private BsonValue GetExpression(BsonExpression expr)
        {
            if (expr == null) return BsonValue.Null;

            return expr.Source;
        }

        #endregion
    }
}