using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace LiteDB
{
    /// <summary>
    /// An IQueryable-like class to write fluent query in LiteDB. Implement same methods from QueryBuilder for strong typed documents
    /// </summary>
    public class LiteQueryable<T>
    {
        private readonly QueryBuilder _query;
        private readonly BsonMapper _mapper;

        internal LiteQueryable(QueryBuilder query, BsonMapper mapper)
        {
            _query = query;
            _mapper = mapper;
        }

        #region Includes

        /// <summary>
        /// Include DBRef field in result query execution
        /// </summary>
        public LiteQueryable<T> Include<K>(Expression<Func<T, K>> dbref)
        {
            _query.Include(_mapper.GetExpression(dbref));
            return this;
        }

        /// <summary>
        /// Include DBRef path in result query execution
        /// </summary>
        public LiteQueryable<T> Include(BsonExpression path)
        {
            _query.Include(path);
            return this;
        }

        /// <summary>
        /// Include DBRef path in result query execution
        /// </summary>
        public LiteQueryable<T> Include(List<BsonExpression> paths)
        {
            foreach(var path in paths)
            {
                _query.Include(path);
            }
            return this;
        }

        #endregion

        #region Where

        /// <summary>
        /// Add new Where expression to final query. Can contains multiple clausules. 
        /// Where("Name LIKE 'John%' AND Age > 20")
        /// </summary>
        public LiteQueryable<T> Where(BsonExpression query)
        {
            _query.Where(query);
            return this;
        }

        /// <summary>
        /// </summary>
        public LiteQueryable<T> Where(BsonExpression query, BsonDocument parameters)
        {
            _query.Where(query, parameters);
            return this;
        }

        /// <summary>
        /// </summary>
        public LiteQueryable<T> Where(BsonExpression query, params BsonValue[] args)
        {
            _query.Where(query, args);
            return this;
        }

        /// <summary>
        /// </summary>
        public LiteQueryable<T> Where(Expression<Func<T, bool>> predicate)
        {
            return this.Where(_mapper.GetExpression(predicate));
        }

        /// <summary>
        /// </summary>
        public LiteQueryable<T> Where(bool condition, BsonExpression query)
        {
            return condition ? this.Where(query) : this;
        }

        /// <summary>
        /// </summary>
        public LiteQueryable<T> Where(bool condition, BsonExpression query, BsonDocument parameters)
        {
            return condition ? this.Where(query, parameters) : this;
        }

        /// <summary>
        /// </summary>
        public LiteQueryable<T> Where(bool condition, BsonExpression query, params BsonValue[] args)
        {
            return condition ? this.Where(query, args) : this;
        }

        /// <summary>
        /// </summary>
        public LiteQueryable<T> Where(bool condition, Expression<Func<T, bool>> predicate)
        {
            return condition ? this.Where(predicate) : this;
        }

        #endregion

        #region Offset/Limit/ForUpdate

        public LiteQueryable<T> ForUpdate()
        {
            _query.ForUpdate();
            return this;
        }

        /// <summary>
        /// Skip N results before starts returing entities
        /// </summary>
        public LiteQueryable<T> Offset(int offset)
        {
            _query.Offset(offset);
            return this;
        }

        /// <summary>
        /// Skip N results before starts returing entities
        /// </summary>
        [Obsolete("Use Offset() method")]
        public LiteQueryable<T> Skip(int offset) => this.Offset(offset);

        /// <summary>
        /// Limit (Take) results 
        /// </summary>
        public LiteQueryable<T> Limit(int limit)
        {
            _query.Limit(limit);
            return this;
        }

        #endregion

        #region OrderBy

        /// <summary>
        /// Sort resultset based on query expression and order. Support only 1 single order by expression
        /// </summary>
        public LiteQueryable<T> OrderBy(BsonExpression query, int order = Query.Ascending)
        {
            _query.OrderBy(query, order);
            return this;
        }

        /// <summary>
        /// Sort resultset based on query expression and order. Support only 1 single order by expression
        /// </summary>
        public LiteQueryable<T> OrderBy<K>(Expression<Func<T, K>> predicate)
        {
            return this.Where(_mapper.GetExpression(predicate));
        }

        #endregion

        #region Select

        public LiteQueryable<T> Select(BsonExpression select)
        {
            _query.Select(select);
            return this;
        }

        public LiteQueryable<K> Select<K>(Expression<Func<T, K>> predicate)
        {
            _query.Select(_mapper.GetExpression(predicate));
            return new LiteQueryable<K>(_query, _mapper);
        }

        public LiteQueryable<T> SelectAll(BsonExpression select)
        {
            _query.Select(select, true);
            return this;
        }

        public LiteQueryable<K> SelectAll<K>(Expression<Func<T, K>> predicate)
        {
            _query.Select(_mapper.GetExpression(predicate), true);
            return new LiteQueryable<K>(_query, _mapper);
        }

        #endregion

        #region GroupBy/Having

        public LiteQueryable<T> GroupBy(BsonExpression groupBy, int order = Query.Ascending)
        {
            _query.GroupBy(groupBy, order);
            return this;
        }

        public LiteQueryable<T> GroupBy<K>(Expression<Func<T, K>> predicate, int order = Query.Ascending)
        {
            _query.GroupBy(_mapper.GetExpression(predicate), order);
            return this;
        }

        public LiteQueryable<T> Having(BsonExpression filter)
        {
            _query.Having(filter);
            return this;
        }

        public LiteQueryable<T> Having<K>(Expression<Func<T, K>> predicate)
        {
            _query.Having(_mapper.GetExpression(predicate));
            return this;
        }
        #endregion

        #region Execute Result

        /// <summary>
        /// Execute query and returns data reader
        /// </summary>
        public BsonDataReader ExecuteReader()
        {
            return _query.ExecuteReader();
        }

        /// <summary>
        /// Execute query and returns data reader
        /// </summary>
        public T ExecuteScalar()
        {
            var value = _query.ExecuteScalar();

            return (T)_mapper.Deserialize(typeof(T), value);
        }

        /// <summary>
        /// Execute explain plan over query to check how engine will execute query
        /// </summary>
        public BsonDocument ExecuteExplainPlan()
        {
            return _query.ExecuteExplainPlan();
        }

        /// <summary>
        /// Execute query returning IEnumerable results.
        /// </summary>
        public IEnumerable<T> ToEnumerable()
        {
            return _query.ToEnumerable().Select(x => _mapper.ToObject<T>(x));
        }

        /// <summary>
        /// Execute query and return results as a List
        /// </summary>
        public List<T> ToList()
        {
            return this.ToEnumerable().ToList();
        }

        /// <summary>
        /// Execute query and return results as an Array
        /// </summary>
        public T[] ToArray()
        {
            return this.ToEnumerable().ToArray();
        }

        /// <summary>
        /// Return query and result only values (not only documents)
        /// </summary>
        public IEnumerable<BsonValue> ToValues()
        {
            return _query.ToValues();
        }

        #endregion

        #region Execute Single/First

        public T Single()
        {
            return this.ToEnumerable().Single();
        }

        public T SingleOrDefault()
        {
            return this.ToEnumerable().SingleOrDefault();
        }

        public T First()
        {
            return this.ToEnumerable().First();
        }

        public T FirstOrDefault()
        {
            return this.ToEnumerable().FirstOrDefault();
        }

        /// <summary>
        /// Return entity by _id key. Throws InvalidOperationException if no document
        /// </summary>
        public T SingleById(BsonValue id)
        {
            return _mapper.ToObject<T>(_query.SingleById(id));
        }

        #endregion

        #region Execute Count

        /// <summary>
        /// Execute Count methos in filter query
        /// </summary>
        public int Count()
        {
            return _query.Count();
        }

        /// <summary>
        /// Returns true/false if filter returns any result
        /// </summary>
        public bool Exists()
        {
            return _query.Exists();
        }

        #endregion

        #region Execute Into

        public int Into(string newCollection, BsonAutoId autoId = BsonAutoId.ObjectId)
        {
            return _query.Into(newCollection, autoId);
        }

        public int Into(IFileCollection file)
        {
            return _query.Into(file);
        }

        #endregion
    }
}