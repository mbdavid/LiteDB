using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Class is a result from optimized QueryBuild. Indicate how engine must run query - there is no more decisions to engine made, must only execute as query was defined
    /// </summary>
    public class Query
    {
        /// <summary>
        /// Index used on query (required)
        /// </summary>
        public Index Index { get; set; } = Index.All("_id", Query.Ascending);

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
        /// Indicate when a query must execute in ascending order
        /// </summary>
        public const int Ascending = 1;

        /// <summary>
        /// Indicate when a query must execute in descending order
        /// </summary>
        public const int Descending = -1;
    }
}