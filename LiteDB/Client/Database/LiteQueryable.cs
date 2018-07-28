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
        /// Load cross reference documents from path expression (DbRef reference)
        /// </summary>
        public LiteQueryable<T> Include<K>(Expression<Func<T, K>> path)
        {
            _query.Include(_mapper.GetExpression(path));
            return this;
        }

        /// <summary>
        /// Load cross reference documents from path expression (DbRef reference)
        /// </summary>
        public LiteQueryable<T> Include(BsonExpression path)
        {
            _query.Include(path);
            return this;
        }

        /// <summary>
        /// Load cross reference documents from path expression (DbRef reference)
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
        /// Filters a sequence of documents based on a predicate expression
        /// </summary>
        public LiteQueryable<T> Where(BsonExpression predicate)
        {
            _query.Where(predicate);
            return this;
        }

        /// <summary>
        /// Filters a sequence of documents based on a predicate expression
        /// </summary>
        public LiteQueryable<T> Where(BsonExpression predicate, BsonDocument parameters)
        {
            _query.Where(predicate, parameters);
            return this;
        }

        /// <summary>
        /// Filters a sequence of documents based on a predicate expression
        /// </summary>
        public LiteQueryable<T> Where(BsonExpression predicate, params BsonValue[] args)
        {
            _query.Where(predicate, args);
            return this;
        }

        /// <summary>
        /// Filters a sequence of documents based on a predicate expression
        /// </summary>
        public LiteQueryable<T> Where(Expression<Func<T, bool>> predicate)
        {
            return this.Where(_mapper.GetExpression(predicate));
        }

        /// <summary>
        /// Filters a sequence of documents based on a predicate expression. Will apply filter only if condition are true
        /// </summary>
        public LiteQueryable<T> Where(bool condition, BsonExpression predicate)
        {
            return condition ? this.Where(predicate) : this;
        }

        /// <summary>
        /// Filters a sequence of documents based on a predicate expression. Will apply filter only if condition are true
        /// </summary>
        public LiteQueryable<T> Where(bool condition, BsonExpression predicate, BsonDocument parameters)
        {
            return condition ? this.Where(predicate, parameters) : this;
        }

        /// <summary>
        /// Filters a sequence of documents based on a predicate expression. Will apply filter only if condition are true
        /// </summary>
        public LiteQueryable<T> Where(bool condition, BsonExpression predicate, params BsonValue[] args)
        {
            return condition ? this.Where(predicate, args) : this;
        }

        /// <summary>
        /// Filters a sequence of documents based on a predicate expression. Will apply filter only if condition are true
        /// </summary>
        public LiteQueryable<T> Where(bool condition, Expression<Func<T, bool>> predicate)
        {
            return condition ? this.Where(predicate) : this;
        }

        #endregion

        #region Offset/Limit/ForUpdate

        /// <summary>
        /// Execute query locking collection in write mode. This is avoid any other thread change results after read document and before transaction ends
        /// </summary>
        public LiteQueryable<T> ForUpdate()
        {
            _query.ForUpdate();
            return this;
        }

        /// <summary>
        /// Bypasses a specified number of documents in resultset and retun the remaining documents (same as Skip)
        /// </summary>
        public LiteQueryable<T> Offset(int offset)
        {
            _query.Offset(offset);
            return this;
        }

        /// <summary>
        /// Bypasses a specified number of documents in resultset and retun the remaining documents (same as Offset)
        /// </summary>
        public LiteQueryable<T> Skip(int offset) => this.Offset(offset);

        /// <summary>
        /// Return a specified number of contiguous documents from start of resultset
        /// </summary>
        public LiteQueryable<T> Limit(int limit)
        {
            _query.Limit(limit);
            return this;
        }

        #endregion

        #region OrderBy

        /// <summary>
        /// Sort the documents of resultset in ascending (or descending) order according to a key (support only one OrderBy)
        /// </summary>
        public LiteQueryable<T> OrderBy(BsonExpression keySelector, int order = Query.Ascending)
        {
            _query.OrderBy(keySelector, order);
            return this;
        }

        /// <summary>
        /// Sort the documents of resultset in ascending (or descending) order according to a key (support only one OrderBy)
        /// </summary>
        public LiteQueryable<T> OrderBy<K>(Expression<Func<T, K>> keySelector, int order = Query.Ascending)
        {
            return this.OrderBy(_mapper.GetExpression(keySelector), order);
        }

        /// <summary>
        /// Sort the documents of resultset in descending order according to a key (support only one OrderBy)
        /// </summary>
        public LiteQueryable<T> OrderByDescending(BsonExpression keySelector) => this.OrderBy(keySelector, Query.Descending);

        /// <summary>
        /// Sort the documents of resultset in descending order according to a key (support only one OrderBy)
        /// </summary>
        public LiteQueryable<T> OrderByDescending<K>(Expression<Func<T, K>> keySelector) => this.OrderBy(keySelector, Query.Descending);

        #endregion

        #region Select

        /// <summary>
        /// Project each document of resultset into a new document/value based on selector expression
        /// </summary>
        public LiteQueryable<T> Select(BsonExpression selector)
        {
            _query.Select(selector);
            return this;
        }

        /// <summary>
        /// Project each document of resultset into a new document/value based on selector expression
        /// </summary>
        public LiteQueryable<K> Select<K>(Expression<Func<T, K>> selector)
        {
            _query.Select(_mapper.GetExpression(selector));
            return new LiteQueryable<K>(_query, _mapper);
        }

        /// <summary>
        /// Project each document of resultset into a new document/value based on selector expression
        /// Apply expression function over all results and will output a single result
        /// </summary>
        public LiteQueryable<T> SelectAll(BsonExpression selector)
        {
            _query.Select(selector, true);
            return this;
        }

        /// <summary>
        /// Project each document of resultset into a new document/value based on selector expression
        /// Apply expression function over all results and will output a single result
        /// </summary>
        public LiteQueryable<K> SelectAll<K>(Expression<Func<T, K>> selector)
        {
            _query.Select(_mapper.GetExpression(selector), true);
            return new LiteQueryable<K>(_query, _mapper);
        }

        #endregion

        #region GroupBy/Having

        /// <summary>
        /// Groups the documents of resultset according to a specified key selector expression (support only one GroupBy)
        /// </summary>
        public LiteQueryable<T> GroupBy(BsonExpression keySelector)
        {
            _query.GroupBy(keySelector);
            return this;
        }

        /// <summary>
        /// Groups the documents of resultset according to a specified key selector expression (support only one GroupBy)
        /// </summary>
        public LiteQueryable<T> GroupBy<K>(Expression<Func<T, K>> keySelector)
        {
            _query.GroupBy(_mapper.GetExpression(keySelector));
            return this;
        }

        /// <summary>
        /// Filter documents after group by pipe according to predicate expression (requires GroupBy and support only one Having)
        /// </summary>
        public LiteQueryable<T> Having(BsonExpression predicate)
        {
            _query.Having(predicate);
            return this;
        }

        /// <summary>
        /// Filter documents after group by pipe according to predicate expression (requires GroupBy and support only one Having)
        /// </summary>
        public LiteQueryable<T> Having(Expression<Func<T, bool>> predicate)
        {
            _query.Having(_mapper.GetExpression(predicate));
            return this;
        }

        #endregion

        #region Execute Result

        /// <summary>
        /// Execute query and returns resultset as generic BsonDataReader
        /// </summary>
        public BsonDataReader ExecuteReader()
        {
            return _query.ExecuteReader();
        }

        /// <summary>
        /// Execute query and return single value
        /// </summary>
        public T ExecuteScalar()
        {
            var value = _query.ExecuteScalar();

            return (T)_mapper.Deserialize(typeof(T), value);
        }

        /// <summary>
        /// Get execution plan over current query definition to see how engine will execute query
        /// </summary>
        public BsonDocument GetPlan()
        {
            return _query.GetPlan();
        }

        /// <summary>
        /// Execute query returning IEnumerable results
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

        /// <summary>
        /// Returns the only document of resultset, and throw an exception if there not exactly one document in the sequence
        /// </summary>
        public T Single()
        {
            return this.ToEnumerable().Single();
        }

        /// <summary>
        /// Returns the only document of resultset, or null if resultset are empty; this method throw an exception if there not exactly one document in the sequence
        /// </summary>
        public T SingleOrDefault()
        {
            return this.ToEnumerable().SingleOrDefault();
        }

        /// <summary>
        /// Returns first document of resultset
        /// </summary>
        public T First()
        {
            return this.ToEnumerable().First();
        }

        /// <summary>
        /// Returns first document of resultset or null if resultset are empty
        /// </summary>
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