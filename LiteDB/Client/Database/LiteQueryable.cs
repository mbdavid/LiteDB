using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace LiteDB
{
    /// <summary>
    /// An IQueryable-like class to write fluent query in LiteDB. Supports Where, OrderBy, GroupBy, Select, Limit/Offset. Execute query as BsonDataReader, IEnumerable, List
    /// </summary>
    public class LiteQueryable<T>
    {
        private readonly ILiteEngine _engine;
        private readonly BsonMapper _mapper;
        private readonly string _collection;
        private readonly QueryDefinition _query;
        private readonly string _uniqueField;

        internal LiteQueryable(ILiteEngine engine, BsonMapper mapper, string collection, QueryDefinition query)
        {
            _engine = engine;
            _mapper = mapper;
            _collection = collection;
            _query = query;
            _uniqueField = collection.StartsWith("$") ? "$" : "_id"; // used in Count/Exists/Any - system collection has no _id field
        }

        #region Includes

        /// <summary>
        /// Load cross reference documents from path expression (DbRef reference)
        /// </summary>
        public LiteQueryable<T> Include<K>(Expression<Func<T, K>> path)
        {
            _query.Includes.Add(_mapper.GetExpression(path));
            return this;
        }

        /// <summary>
        /// Load cross reference documents from path expression (DbRef reference)
        /// </summary>
        public LiteQueryable<T> Include(BsonExpression path)
        {
            _query.Includes.Add(path);
            return this;
        }

        /// <summary>
        /// Load cross reference documents from path expression (DbRef reference)
        /// </summary>
        public LiteQueryable<T> Include(List<BsonExpression> paths)
        {
            _query.Includes.AddRange(paths);
            return this;
        }

        #endregion

        #region Where

        /// <summary>
        /// Filters a sequence of documents based on a predicate expression
        /// </summary>
        public LiteQueryable<T> Where(BsonExpression predicate)
        {
            _query.Where.Add(predicate);
            return this;
        }

        /// <summary>
        /// Filters a sequence of documents based on a predicate expression
        /// </summary>
        public LiteQueryable<T> Where(string predicate, BsonDocument parameters)
        {
            _query.Where.Add(BsonExpression.Create(predicate, parameters));
            return this;
        }

        /// <summary>
        /// Filters a sequence of documents based on a predicate expression
        /// </summary>
        public LiteQueryable<T> Where(string predicate, params BsonValue[] args)
        {
            _query.Where.Add(BsonExpression.Create(predicate, args));
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
            _query.ForUpdate = true;
            return this;
        }

        /// <summary>
        /// Bypasses a specified number of documents in resultset and retun the remaining documents (same as Skip)
        /// </summary>
        public LiteQueryable<T> Offset(int offset)
        {
            _query.Offset = offset;
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
            _query.Limit = limit;
            return this;
        }

        #endregion

        #region OrderBy

        /// <summary>
        /// Sort the documents of resultset in ascending (or descending) order according to a key (support only one OrderBy)
        /// </summary>
        public LiteQueryable<T> OrderBy(BsonExpression keySelector, int order = Query.Ascending)
        {
            _query.OrderBy = keySelector;
            _query.Order = order;
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
        public LiteQueryable<BsonDocument> Select(BsonExpression selector)
        {
            _query.Select = selector;

            return new LiteQueryable<BsonDocument>(_engine, _mapper, _collection, _query);
        }

        /// <summary>
        /// Project each document of resultset into a new document/value based on selector expression
        /// </summary>
        public LiteQueryable<K> Select<K>(Expression<Func<T, K>> selector) where K : class
        {
            _query.Select = _mapper.GetExpression(selector);

            return new LiteQueryable<K>(_engine, _mapper, _collection, _query);
        }

        /// <summary>
        /// Project each document of resultset into a new document/value based on selector expression
        /// Apply expression function over all results and will output a single result
        /// </summary>
        public LiteQueryable<BsonDocument> SelectAll(BsonExpression selector)
        {
            _query.Select = selector;
            _query.SelectAll = true;

            return new LiteQueryable<BsonDocument>(_engine, _mapper, _collection, _query);
        }

        /// <summary>
        /// Project each document of resultset into a new document/value based on selector expression
        /// Apply expression function over all results and will output a single result
        /// </summary>
        public LiteQueryable<K> SelectAll<K>(Expression<Func<T, K>> selector)
        {
            _query.Select = _mapper.GetExpression(selector);
            _query.SelectAll = true;

            return new LiteQueryable<K>(_engine, _mapper, _collection, _query);
        }

        #endregion

        #region GroupBy/Having

        /// <summary>
        /// Groups the documents of resultset according to a specified key selector expression (support only one GroupBy)
        /// </summary>
        public LiteQueryable<T> GroupBy(BsonExpression keySelector)
        {
            _query.GroupBy = keySelector;
            return this;
        }

        /// <summary>
        /// Groups the documents of resultset according to a specified key selector expression (support only one GroupBy)
        /// </summary>
        public LiteQueryable<T> GroupBy<K>(Expression<Func<T, K>> keySelector)
        {
            _query.GroupBy = _mapper.GetExpression(keySelector);
            return this;
        }

        /// <summary>
        /// Filter documents after group by pipe according to predicate expression (requires GroupBy and support only one Having)
        /// </summary>
        public LiteQueryable<T> Having(BsonExpression predicate)
        {
            _query.Having = predicate;
            return this;
        }

        /// <summary>
        /// Filter documents after group by pipe according to predicate expression (requires GroupBy and support only one Having)
        /// </summary>
        public LiteQueryable<T> Having(Expression<Func<T, bool>> predicate)
        {
            _query.Having = _mapper.GetExpression(predicate);
            return this;
        }

        #endregion

        #region Execute Result

        /// <summary>
        /// Execute query and returns resultset as generic BsonDataReader
        /// </summary>
        public IBsonDataReader ExecuteReader()
        {
            _query.ExplainPlan = false;

            return _engine.Query(_collection, _query);
        }

        /// <summary>
        /// Run reader and resulta IEnumerable of documents
        /// </summary>
        public IEnumerable<BsonDocument> ToDocuments()
        {
            using (var reader = this.ExecuteReader())
            {
                while (reader.Read())
                {
                    yield return reader.Current.AsDocument;
                }
            }
        }

        /// <summary>
        /// Execute query and return single value
        /// </summary>
        public T ExecuteScalar()
        {
            var value = this.ToDocuments().FirstOrDefault();

            if (value == null) return default(T);

            return (T)_mapper.Deserialize(typeof(T), value);
        }

        /// <summary>
        /// Get execution plan over current query definition to see how engine will execute query
        /// </summary>
        public BsonDocument GetPlan()
        {
            _query.ExplainPlan = true;

            using (var reader = _engine.Query(_collection, _query))
            {
                return reader.Current.AsDocument;
            }
        }

        /// <summary>
        /// Execute query returning IEnumerable results
        /// </summary>
        public IEnumerable<T> ToEnumerable()
        {
            return this.ToDocuments().Select(x => (T)_mapper.Deserialize(typeof(T), x));
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

        #endregion

        #region Execute Count

        /// <summary>
        /// Execute Count methos in filter query
        /// </summary>
        public int Count()
        {
            this.SelectAll($"{{ count: COUNT({_uniqueField}) }}");

            return this.ToDocuments().Single()["count"].AsInt32;
        }

        /// <summary>
        /// Execute Count methos in filter query
        /// </summary>
        public long LongCount()
        {
            this.SelectAll($"{{ count: COUNT({_uniqueField}) }}");

            return this.ToDocuments().Single()["count"].AsInt64;
        }

        /// <summary>
        /// Returns true/false if query returns any result
        /// </summary>
        public bool Exists()
        {
            this.SelectAll($"{{ exists: ANY({_uniqueField} != null) }}");

            return this.ToDocuments().Single()["exists"].AsBoolean;
        }

        #endregion

        #region Execute Into

        public int Into(string newCollection, BsonAutoId autoId = BsonAutoId.ObjectId)
        {
            _query.Into = newCollection;
            _query.IntoAutoId = autoId;

            using (var reader = this.ExecuteReader())
            {
                return reader.Current.AsInt32;
            }
        }

        #endregion
    }
}