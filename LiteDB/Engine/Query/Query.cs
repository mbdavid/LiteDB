using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Class that represent an query in database. This class is produced by query analyzer
    /// </summary>
    public class Query
    {
        /// <summary>
        /// Index used on query
        /// </summary>
        public Index Index { get; set; } = Index.All("_id", Query.Ascending);
        public bool KeyOnly { get; set; } = false;
        public bool ForUpdate { get; set; } = false; // faz lock de escrita
        public List<BsonExpression> Filters { get; set; }
        public List<BsonExpression> IncludeBefore { get; set; }
        public List<BsonExpression> IncludeAfter { get; set; }
        public List<BsonExpression> OrderBy { get; set; }
        public BsonExpression GroupBy { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
        public BsonExpression Select { get; set; } = new BsonExpression("$");

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