using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Class to analyze query builder to select best index options or include new index if needed
    /// </summary>
    internal class QueryAnalyzer
    {
        private Snapshot _snapshot;
        private Query _query;
        private List<BsonExpression> _where;

        public QueryAnalyzer(Snapshot snapshot, Query query, List<BsonExpression> where)
        {
            _snapshot = snapshot;
            _query = query;
            _where = where;
        }

        /// <summary>
        /// Execute collection analyze
        /// </summary>
        public void RunAnalyzer()
        {
            // if index already defined (by user or already run analyze) just exit - there is no changes to do
            if (_query.Index != null) return;

        }
    }
}