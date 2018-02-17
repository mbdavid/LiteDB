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
        private QueryPlan _query;
        private List<BsonExpression> _where;
        private bool _autoIndex;
        private bool _countOnly;

        public QueryAnalyzer(Snapshot snapshot, QueryPlan query, List<BsonExpression> where, bool countOnly, bool autoIndex)
        {
            _snapshot = snapshot;
            _query = query;
            _where = where;
            _countOnly = countOnly;
            _autoIndex = autoIndex;
        }

        /// <summary>
        /// Execute collection analyze
        /// </summary>
        public void RunAnalyzer()
        {
            // if index already defined (by user or already run analyzed) just exit - there is no changes to do
            if (_query.Index != null) return;



        }
    }
}