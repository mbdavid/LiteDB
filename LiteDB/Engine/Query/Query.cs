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
        /// Indicate when a query must execute in ascending order
        /// </summary>
        public const int Ascending = 1;

        /// <summary>
        /// Indicate when a query must execute in descending order
        /// </summary>
        public const int Descending = -1;
    }
}