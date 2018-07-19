using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LiteDB.Engine
{
    /// <summary>
    /// Internal class to parse and execute sql-like commands
    /// </summary>
    public class SqlKeywords
    {
        /// <summary>
        /// Get all methods names
        /// </summary>
        public static string[] Methods = typeof(BsonExpressionMethods).GetMethods(BindingFlags.Public | BindingFlags.Static).Select(x => x.Name).ToArray();

        public static string[] Keywords = new[]
        {
            "BETWEEN", "IN", "AND", "OR",
            "BEGIN", "TRANS", "TRANSACTION", "COMMIT", "ROLLBACK",
            "ANALYZE",
            "CHECKPOINT",
            "CREATE", "INDEX", "ON", "UNIQUE",
            "DELETE",
            "DROP", "COLLECTION",
            "RENAME", "TO",
            "INSERT", "INTO", "VALUES",
            "SELECT", "ALL", "FROM", "WHERE", "INCLUDE", "GROUP", "ORDER", "BY", "ASC", "DESC", "HAVING", "LIMIT", "OFFSET", "FOR",
            "SET", "EXPLAIN",
            "SHRINK",
            "UPDATE", "REPLACE",
            "VACCUM",
            "TRUE", "FALSE", "NULL"
        };
    }
}