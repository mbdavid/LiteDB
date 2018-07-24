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
        /// Filters a sequence of documents based on a predicate expression
        /// </summary>
        public QueryBuilder Where(BsonExpression predicate)
        {
            // add expression in where list breaking AND statments
            if (predicate.IsPredicate || predicate.Type == BsonExpressionType.Or)
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
                throw LiteException.InvalidExpressionTypePredicate(predicate);
            }

            return this;
        }

        /// <summary>
        /// Filters a sequence of documents based on a predicate expression
        /// </summary>
        public QueryBuilder Where(string predicate, params BsonValue[] args)
        {
            return this.Where(BsonExpression.Create(predicate, args));
        }

        /// <summary>
        /// Filters a sequence of documents based on a predicate expression
        /// </summary>
        public QueryBuilder Where(string predicate, BsonDocument parameters)
        {
            return this.Where(BsonExpression.Create(predicate, parameters));
        }

        /// <summary>
        /// Load cross reference documents from path expression (DbRef reference)
        /// </summary>
        public QueryBuilder Include(BsonExpression path)
        {
            if (path == null) throw new NullReferenceException(nameof(path));

            if (path.Type != BsonExpressionType.Path) throw LiteException.InvalidExpressionType(path, BsonExpressionType.Path);

            _includes.Add(path);

            return this;
        }

        /// <summary>
        /// Sort the documents of resultset in ascending (or descending) order according to a key (support only one OrderBy)
        /// </summary>
        public QueryBuilder OrderBy(BsonExpression keySelector, int order = Query.Ascending)
        {
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (_orderBy != null) throw new InvalidOperationException("ORDER BY already defined");

            _orderBy = new OrderBy(keySelector, order);

            return this;
        }

        /// <summary>
        /// Sort the documents of resultset in descending order according to a key (support only one OrderBy)
        /// </summary>
        public QueryBuilder OrderByDescending(BsonExpression keySelector) => this.OrderBy(keySelector, Query.Descending);

        /// <summary>
        /// Groups the documents of resultset according to a specified key selector expression (support only one GroupBy)
        /// </summary>
        public QueryBuilder GroupBy(BsonExpression keySelector, int order = Query.Ascending)
        {
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (_groupBy != null) throw new InvalidOperationException("GROUP BY already defined");

            _groupBy = new GroupBy(keySelector, order);

            _groupBy.Select = _select?.Expression;
            _select = null;

            return this;
        }

        /// <summary>
        /// Filter documents after group by pipe according to predicate expression (requires GroupBy and support only one Having)
        /// </summary>
        public QueryBuilder Having(BsonExpression predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (_groupBy == null) throw new InvalidOperationException("HAVING need GROUP BY expression");
            if (_groupBy.Having != null) throw new InvalidOperationException("HAVING already defined");

            _groupBy.Having = predicate;

            return this;
        }

        /// <summary>
        /// Return a specified number of contiguous documents from start of resultset
        /// </summary>
        public QueryBuilder Limit(int limit)
        {
            _limit = limit;

            return this;
        }

        /// <summary>
        /// Bypasses a specified number of documents in resultset and retun the remaining documents
        /// </summary>
        public QueryBuilder Offset(int offset)
        {
            _offset = offset;

            return this;
        }

        /// <summary>
        /// Project each document of resultset into a new document/value based on selector expression
        /// </summary>
        public QueryBuilder Select(BsonExpression selector) => this.Select(selector, false);

        /// <summary>
        /// Project each document of resultset into a new document/value based on selector expression
        /// Apply expression function over all results and will output a single result
        /// </summary>
        public QueryBuilder SelectAll(BsonExpression selector) => this.Select(selector, true);

        /// <summary>
        /// Project each document of resultset into a new document/value based on selector expression. 
        /// If all = true: apply expression function over all results and will output a single result
        /// </summary>
        internal QueryBuilder Select(BsonExpression selector, bool all = false)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (_select != null) throw new InvalidOperationException("SELECT already defined");

            if (_groupBy != null)
            {
                _groupBy.Select = selector;
            }
            else
            {
                _select = new Select(selector, all);
            }

            return this;
        }

        /// <summary>
        /// Execute query locking collection in write mode. This is avoid any other thread change results after read document and before transaction ends
        /// </summary>
        public QueryBuilder ForUpdate()
        {
            _forUpdate = true;

            return this;
        }

        /// <summary>
        /// Define your own index predicate expression to run over collection. 
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