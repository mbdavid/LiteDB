using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    /// <summary>
    /// Class to provider a fluent query API to complex queries. This class will be optimied to convert into Query class before run
    /// </summary>
    public partial class QueryBuilder
    {
        private readonly LiteEngine _engine;
        private readonly string _collection;

        private Index _index = null;

        private List<BsonExpression> _where = new List<BsonExpression>();
        private List<BsonExpression> _includes = new List<BsonExpression>();

        private OrderBy _orderBy = null;
        private GroupBy _groupBy = null;

        private Select _select = null;

        private int _offset = 0;
        private int _limit = int.MaxValue;
        private bool _forUpdate = false;

        /// <summary>
        /// Initalize QueryBuilder with a database collection
        /// </summary>
        public QueryBuilder(string collection, LiteEngine engine)
        {
            _collection = collection;
            _engine = engine;
        }

        /// <summary>
        /// Initialize QueryBuilder using an virtual collection
        /// </summary>
        public QueryBuilder(string collection, LiteEngine engine, IEnumerable<BsonDocument> source)
            : this(collection, engine)
        {
            _index = new IndexVirtual(source);
        }

        /// <summary>
        /// Add new WHERE statement in your query. Can be executed with an index or via full scan
        /// </summary>
        public QueryBuilder Where(BsonExpression predicate)
        {
            // add expression in where list breaking AND statments
            if (predicate.IsConditional || predicate.Type == BsonExpressionType.Or)
            {
                _where.Add(predicate);
            }
            else if(predicate.Type == BsonExpressionType.And)
            {
                var left = predicate.Left;
                var right = predicate.Right;

                left.Parameters.Extend(predicate.Parameters);
                right.Parameters.Extend(predicate.Parameters);

                this.Where(left);
                this.Where(right);
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
        /// </summary>
        public QueryBuilder Include(BsonExpression path)
        {
            if (path == null) throw new NullReferenceException(nameof(path));

            if (path.Type != BsonExpressionType.Path) throw LiteException.InvalidExpressionType(path, BsonExpressionType.Path);

            _includes.Add(path);

            return this;
        }

        /// <summary>
        /// Add order by on your result. OrderBy paramter can be an expression
        /// </summary>
        public QueryBuilder OrderBy(BsonExpression orderBy, int order = Query.Ascending)
        {
            if (orderBy == null) throw new ArgumentNullException(nameof(orderBy));
            if (_orderBy != null) throw new InvalidOperationException("ORDER BY already defined");

            _orderBy = new OrderBy(orderBy, order);

            return this;
        }

        /// <summary>
        /// Define GroupBy expression
        /// </summary>
        public QueryBuilder GroupBy(BsonExpression groupBy, int order = Query.Ascending)
        {
            if (groupBy == null) throw new ArgumentNullException(nameof(groupBy));
            if (_groupBy != null) throw new InvalidOperationException("GROUP BY already defined");

            _groupBy = new GroupBy(groupBy, order);

            _groupBy.Select = _select?.Expression;
            _select = null;

            return this;
        }

        /// <summary>
        /// Define Having filter expression (need GroupBy definition)
        /// </summary>
        public QueryBuilder Having(BsonExpression filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (_groupBy == null) throw new InvalidOperationException("HAVING need GROUP BY expression");
            if (_groupBy.Having != null) throw new InvalidOperationException("HAVING already defined");

            _groupBy.Having = filter;

            return this;
        }

        /// <summary>
        /// Limit your resultset
        /// </summary>
        public QueryBuilder Limit(int limit)
        {
            _limit = limit;

            return this;
        }

        /// <summary>
        /// Skip/offset your resultset
        /// </summary>
        public QueryBuilder Offset(int offset)
        {
            _offset = offset;

            return this;
        }

        /// <summary>
        /// Transform your output document using this select expression.
        /// </summary>
        public QueryBuilder Select(BsonExpression select, bool aggregate = false)
        {
            if (select == null) throw new ArgumentNullException(nameof(select));
            if (_select != null) throw new InvalidOperationException("SELECT already defined");

            if (_groupBy != null)
            {
                _groupBy.Select = select;
            }
            else
            {
                _select = new Select(select, false);
            }

            return this;
        }

        /// <summary>
        /// Execute query locking collection in write mode. This is avoid any other thread change results after read document and before transaction ends.
        /// </summary>
        public QueryBuilder ForUpdate()
        {
            _forUpdate = true;

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

            _index = index;

            return this;
        }
    }
}