using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace LiteDB
{
    /// <summary>
    /// An IQueryable-like class to write fluent query in documents in collection.
    /// </summary>
    public class LiteQueryable<T> : ILiteQueryableWithIncludes<T>
    {
        private readonly ILiteEngine _engine;
        private readonly BsonMapper _mapper;
        private readonly string _collection;
        private readonly QueryDefinition _query;

        // indicate that T type are simple and result are inside first document fields (query always return a BsonDocument)
        private readonly bool _isSimpleType = typeof(T).IsValueType || typeof(T) == typeof(string);

        internal LiteQueryable(ILiteEngine engine, BsonMapper mapper, string collection, QueryDefinition query)
        {
            _engine = engine;
            _mapper = mapper;
            _collection = collection;
            _query = query;
        }

        #region Includes

        /// <summary>
        /// Load cross reference documents from path expression (DbRef reference)
        /// </summary>
        public ILiteQueryableWithIncludes<T> Include<K>(Expression<Func<T, K>> path)
        {
            _query.Includes.Add(_mapper.GetExpression(path));
            return this;
        }

        /// <summary>
        /// Load cross reference documents from path expression (DbRef reference)
        /// </summary>
        public ILiteQueryableWithIncludes<T> Include(BsonExpression path)
        {
            _query.Includes.Add(path);
            return this;
        }

        /// <summary>
        /// Load cross reference documents from path expression (DbRef reference)
        /// </summary>
        public ILiteQueryableWithIncludes<T> Include(List<BsonExpression> paths)
        {
            _query.Includes.AddRange(paths);
            return this;
        }

        #endregion

        #region Where

        /// <summary>
        /// Filters a sequence of documents based on a predicate expression
        /// </summary>
        public ILiteQueryable<T> Where(BsonExpression predicate)
        {
            _query.Where.Add(predicate);
            return this;
        }

        /// <summary>
        /// Filters a sequence of documents based on a predicate expression
        /// </summary>
        public ILiteQueryable<T> Where(string predicate, BsonDocument parameters)
        {
            _query.Where.Add(BsonExpression.Create(predicate, parameters));
            return this;
        }

        /// <summary>
        /// Filters a sequence of documents based on a predicate expression
        /// </summary>
        public ILiteQueryable<T> Where(string predicate, params BsonValue[] args)
        {
            _query.Where.Add(BsonExpression.Create(predicate, args));
            return this;
        }

        /// <summary>
        /// Filters a sequence of documents based on a predicate expression
        /// </summary>
        public ILiteQueryable<T> Where(Expression<Func<T, bool>> predicate)
        {
            return this.Where(_mapper.GetExpression(predicate));
        }

        #endregion

        #region GroupBy

        /// <summary>
        /// Groups the documents of resultset according to a specified key selector expression (support only one GroupBy)
        /// </summary>
        public ILiteQueryableFiltered<T> GroupBy(BsonExpression keySelector)
        {
            _query.GroupBy = keySelector;
            return this;
        }

        /// <summary>
        /// Groups the documents of resultset according to a specified key selector expression (support only one GroupBy)
        /// </summary>
        public ILiteQueryableFiltered<T> GroupBy<K>(Expression<Func<T, K>> keySelector)
        {
            _query.GroupBy = _mapper.GetExpression(keySelector);
            return this;
        }

        #endregion

        #region Having

        /// <summary>
        /// Filter documents after group by pipe according to predicate expression (requires GroupBy and support only one Having)
        /// </summary>
        public ILiteQueryableSelected<T> Having(BsonExpression predicate)
        {
            _query.Having = predicate;
            return this;
        }

        /// <summary>
        /// Filter documents after group by pipe according to predicate expression (requires GroupBy and support only one Having)
        /// </summary>
        public ILiteQueryableSelected<T> Having(Expression<Func<T, bool>> predicate)
        {
            _query.Having = _mapper.GetExpression(predicate);
            return this;
        }

        #endregion

        #region OrderBy

        /// <summary>
        /// Sort the documents of resultset in ascending (or descending) order according to a key (support only one OrderBy)
        /// </summary>
        public ILiteQueryableOrdered<T> OrderBy(BsonExpression keySelector, int order = Query.Ascending)
        {
            _query.OrderBy = keySelector;
            _query.Order = order;
            return this;
        }

        /// <summary>
        /// Sort the documents of resultset in ascending (or descending) order according to a key (support only one OrderBy)
        /// </summary>
        public ILiteQueryableOrdered<T> OrderBy<K>(Expression<Func<T, K>> keySelector, int order = Query.Ascending)
        {
            return this.OrderBy(_mapper.GetExpression(keySelector), order);
        }

        /// <summary>
        /// Sort the documents of resultset in descending order according to a key (support only one OrderBy)
        /// </summary>
        public ILiteQueryableOrdered<T> OrderByDescending(BsonExpression keySelector) => this.OrderBy(keySelector, Query.Descending);

        /// <summary>
        /// Sort the documents of resultset in descending order according to a key (support only one OrderBy)
        /// </summary>
        public ILiteQueryableOrdered<T> OrderByDescending<K>(Expression<Func<T, K>> keySelector) => this.OrderBy(keySelector, Query.Descending);

        #endregion

        #region Select

        /// <summary>
        /// Project each document of resultset into a new document/value based on selector expression
        /// </summary>
        public ILiteQueryableSelected<BsonDocument> Select(BsonExpression selector)
        {
            _query.Select = selector;

            return new LiteQueryable<BsonDocument>(_engine, _mapper, _collection, _query);
        }

        /// <summary>
        /// Project each document of resultset into a new document/value based on selector expression
        /// </summary>
        public ILiteQueryableSelected<K> Select<K>(Expression<Func<T, K>> selector)
        {
            _query.Select = _mapper.GetExpression(selector);

            return new LiteQueryable<K>(_engine, _mapper, _collection, _query);
        }

        #endregion

        #region Offset/Limit/ForUpdate

        /// <summary>
        /// Execute query locking collection in write mode. This is avoid any other thread change results after read document and before transaction ends
        /// </summary>
        public ILiteQueryableOrdered<T> ForUpdate()
        {
            _query.ForUpdate = true;
            return this;
        }

        /// <summary>
        /// Bypasses a specified number of documents in resultset and retun the remaining documents (same as Skip)
        /// </summary>
        public ILiteQueryableOrdered<T> Offset(int offset)
        {
            _query.Offset = offset;
            return this;
        }

        /// <summary>
        /// Bypasses a specified number of documents in resultset and retun the remaining documents (same as Offset)
        /// </summary>
        public ILiteQueryableOrdered<T> Skip(int offset) => this.Offset(offset);

        /// <summary>
        /// Return a specified number of contiguous documents from start of resultset
        /// </summary>
        public ILiteQueryableOrdered<T> Limit(int limit)
        {
            _query.Limit = limit;
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
        /// Execute query and return resultset as IEnumerable of documents
        /// </summary>
        public IEnumerable<BsonDocument> ToDocuments()
        {
            using (var reader = this.ExecuteReader())
            {
                while (reader.Read())
                {
                    yield return reader.Current as BsonDocument;
                }
            }
        }

        /// <summary>
        /// Execute query and return resultset as IEnumerable of T. If T is a ValueType or String, return values only (not documents)
        /// </summary>
        public IEnumerable<T> ToEnumerable()
        {
            if (_isSimpleType)
            {
                return this.ToDocuments()
                    .Select(x => x[x.Keys.First()])
                    .Select(x => (T)_mapper.Deserialize(typeof(T), x));
            }
            else
            {
                return this.ToDocuments()
                    .Select(x => (T)_mapper.Deserialize(typeof(T), x));
            }
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
            this.Select($"{{ count: COUNT(*) }}");

            return this.ToDocuments().Single()["count"].AsInt32;
        }

        /// <summary>
        /// Execute Count methos in filter query
        /// </summary>
        public long LongCount()
        {
            this.Select($"{{ count: COUNT(*) }}");

            return this.ToDocuments().Single()["count"].AsInt64;
        }

        /// <summary>
        /// Returns true/false if query returns any result
        /// </summary>
        public bool Exists()
        {
            this.Select($"{{ exists: ANY(*) }}");

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