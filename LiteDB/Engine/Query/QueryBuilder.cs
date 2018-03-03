using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Class to provider a fluent query API to complex queries. This class will be optimied to convert into Query class before run
    /// </summary>
    public partial class QueryBuilder
    {
        private string _collection;
        private LiteEngine _engine;

        private QueryPlan _query = new QueryPlan();
        private List<BsonExpression> _where = new List<BsonExpression>();
        private BsonExpression _orderBy = null;
        private int _order = Query.Ascending;

        public QueryBuilder()
        {
        }

        public QueryBuilder(string collection, LiteEngine engine)
        {
            _collection = collection;
            _engine = engine;
        }

        /// <summary>
        /// Add new WHERE statement in your query. Can be executed with an index or via full scan
        /// </summary>
        public QueryBuilder Where(BsonExpression predicate)
        {
            if (_optimized) throw new InvalidOperationException("Where() is not avaiable in executed query");

            // add expression in where list breaking AND statments
            if (predicate.IsConditional || predicate.Type == BsonExpressionType.Or)
            {
                _where.Add(predicate);
            }
            else if(predicate.Type == BsonExpressionType.And)
            {
                this.Where(predicate.Left);
                this.Where(predicate.Right);
            }
            else
            {
                throw LiteException.InvalidExpressionTypeConditional(predicate);
            }

            return this;
        }

        /// <summary>
        /// Add new WHERE statement in your query. Can be executed with an index or via full scan
        /// </summary>
        public QueryBuilder Where(string predicate, params BsonValue[] args)
        {
            return this.Where(BsonExpression.Create(predicate, args));
        }

        /// <summary>
        /// Add new WHERE statement in your query. Can be executed with an index or via full scan
        /// </summary>
        public QueryBuilder Where(string predicate, BsonDocument parameters)
        {
            return this.Where(BsonExpression.Create(predicate, parameters));
        }

        /// <summary>
        /// Load cross reference documents from path expression (DbRef reference). 
        /// If called before Where() will load references before filter (worst). If called after Where() will load references only in filtered results.
        /// Use before Where only if you need add this include in filter expression
        /// </summary>
        public QueryBuilder Include(BsonExpression path)
        {
            if (path == null) throw new NullReferenceException(nameof(path));
            if (_optimized) throw new InvalidOperationException("Include() is not avaiable in executed query");

            if (path.Type == BsonExpressionType.Path) throw LiteException.InvalidExpressionType(path, BsonExpressionType.Path);

            if (_where.Count == 0)
            {
                _query.IncludeBefore.Add(path);
            }
            else
            {
                _query.IncludeAfter.Add(path);
            }

            return this;
        }

        /// <summary>
        /// Load cross reference documents from path expression (DbRef reference). 
        /// If called before Where() will load references before filter (worst). If called after Where() will load references only in filtered results.
        /// Use before Where only if you need add this include in filter expression
        /// </summary>
        public QueryBuilder Include(string path)
        {
            return this.Include(BsonExpression.Create(path));
        }

        /// <summary>
        /// Add order by on your result. OrderBy paramter can be an expression
        /// </summary>
        public QueryBuilder OrderBy(BsonExpression orderBy, int order = Query.Ascending)
        {
            if (orderBy == null) throw new ArgumentNullException(nameof(orderBy));
            if (_optimized) throw new InvalidOperationException("OrderBy() is not avaiable in executed query");

            _orderBy = orderBy;
            _order = order;

            return this;
        }

        /// <summary>
        /// Add order by on your result. OrderBy paramter can be an expression
        /// </summary>
        public QueryBuilder OrderBy(string orderBy, int order = Query.Ascending)
        {
            return this.OrderBy(BsonExpression.Create(orderBy), order);
        }

        /// <summary>
        /// Define GroupBy expression
        /// </summary>
        public QueryBuilder GroupBy(BsonExpression groupBy, int order = Query.Ascending)
        {
            if (groupBy == null) throw new ArgumentNullException(nameof(groupBy));
            if (_optimized) throw new InvalidOperationException("GroupBy() is not avaiable in executed query");

            _query.GroupBy = groupBy;
            _query.GroupByOrder = order;

            return this;
        }

        /// <summary>
        /// Define GroupBy expression
        /// </summary>
        public QueryBuilder GroupBy(string groupBy, int order = Query.Ascending)
        {
            return this.GroupBy(BsonExpression.Create(groupBy), order);
        }

        /// <summary>
        /// Limit your resultset
        /// </summary>
        public QueryBuilder Limit(int limit)
        {
            _query.Limit = limit;

            return this;
        }

        /// <summary>
        /// Skip/offset your resultset
        /// </summary>
        public QueryBuilder Offset(int offset)
        {
            _query.Offset = offset;

            return this;
        }

        /// <summary>
        /// Transform your output document using this select expression
        /// </summary>
        public QueryBuilder Select(BsonExpression select)
        {
            if (select == null) throw new ArgumentNullException(nameof(select));
            if (_optimized) throw new InvalidOperationException("Select() is not avaiable in executed query");

            _query.Select = select;

            return this;
        }

        /// <summary>
        /// Transform your output document using this select expression
        /// </summary>
        public QueryBuilder Select(string select)
        {
            return this.Select(BsonExpression.Create(select));
        }

        /// <summary>
        /// If use keyOnly = true, do not load document - use only index key
        /// </summary>
        public QueryBuilder Select(bool keyOnly)
        {
            if (_optimized) throw new InvalidOperationException("Select() is not avaiable in executed query");

            _query.KeyOnly = keyOnly;

            return this;
        }

        /// <summary>
        /// Execute query locking collection in write mode. This is avoid any other thread change results after read document and before transaction ends.
        /// </summary>
        public QueryBuilder ForUpdate()
        {
            _query.ForUpdate = true;

            return this;
        }

        /// <summary>
        /// Define your own index conditional expression to run over collection. 
        /// If not defined (default), optimization will be auto select best option or create a new one.
        /// Use this option only if you want define index and do not use optimize function.
        /// </summary>
        public QueryBuilder Index(Index index)
        {
            if (index == null) throw new ArgumentNullException(nameof(index));
            if (_optimized) throw new InvalidOperationException("Index() is not avaiable in executed query");

            _query.Index = index;

            return this;
        }
    }
}