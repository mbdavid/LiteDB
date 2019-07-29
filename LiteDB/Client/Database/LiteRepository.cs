using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using static LiteDB.Constants;

namespace LiteDB
{
    /// <summary>
    /// The LiteDB repository pattern. A simple way to access your documents in a single class with fluent query api
    /// </summary>
    public class LiteRepository : IDisposable
    {
        #region Properties

        private readonly LiteDatabase _db = null;

        /// <summary>
        /// Get database instance
        /// </summary>
        public LiteDatabase Database { get { return _db; } }

        #endregion

        #region Ctor

        /// <summary>
        /// Starts LiteDB database using a connection string for file system database
        /// </summary>
        public LiteRepository(string connectionString, BsonMapper mapper = null)
        {
            _db = new LiteDatabase(connectionString, mapper);
        }

        /// <summary>
        /// Starts LiteDB database using a connection string for file system database
        /// </summary>
        public LiteRepository(ConnectionString connectionString, BsonMapper mapper = null)
        {
            _db = new LiteDatabase(connectionString, mapper);
        }

        /// <summary>
        /// Starts LiteDB database using a Stream disk
        /// </summary>
        public LiteRepository(Stream stream, BsonMapper mapper = null)
        {
            _db = new LiteDatabase(stream, mapper);
        }

        #endregion

        #region Shortchut from FileStorage

        // /// <summary>
        // /// Returns a special collection for storage files/stream inside datafile
        // /// </summary>
        // public LiteStorage FileStorage
        // {
        //     get { return _db.FileStorage; }
        // }

        #endregion

        #region Insert

        /// <summary>
        /// Insert a new document into collection. Document Id must be a new value in collection - Returns document Id
        /// </summary>
        public void Insert<T>(T entity, string collectionName = null)
        {
            _db.GetCollection<T>(collectionName).Insert(entity);
        }

        /// <summary>
        /// Insert an array of new documents into collection. Document Id must be a new value in collection. Can be set buffer size to commit at each N documents
        /// </summary>
        public int Insert<T>(IEnumerable<T> entities, string collectionName = null)
        {
            return _db.GetCollection<T>(collectionName).Insert(entities);
        }

        #endregion

        #region Update

        /// <summary>
        /// Update a document into collection. Returns false if not found document in collection
        /// </summary>
        public bool Update<T>(T entity, string collectionName = null)
        {
            return _db.GetCollection<T>(collectionName).Update(entity);
        }

        /// <summary>
        /// Update all documents
        /// </summary>
        public int Update<T>(IEnumerable<T> entities, string collectionName = null)
        {
            return _db.GetCollection<T>(collectionName).Update(entities);
        }

        #endregion

        #region Upsert

        /// <summary>
        /// Insert or Update a document based on _id key. Returns true if insert entity or false if update entity
        /// </summary>
        public bool Upsert<T>(T entity, string collectionName = null)
        {
            return _db.GetCollection<T>(collectionName).Upsert(entity);
        }

        /// <summary>
        /// Insert or Update all documents based on _id key. Returns entity count that was inserted
        /// </summary>
        public int Upsert<T>(IEnumerable<T> entities, string collectionName = null)
        {
            return _db.GetCollection<T>(collectionName).Upsert(entities);
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete entity based on _id key
        /// </summary>
        public bool Delete<T>(BsonValue id, string collectionName = null)
        {
            return _db.GetCollection<T>(collectionName).Delete(id);
        }

        /// <summary>
        /// Delete entity based on Query
        /// </summary>
        public int DeleteMany<T>(BsonExpression predicate, string collectionName = null)
        {
            return _db.GetCollection<T>(collectionName).DeleteMany(predicate);
        }

        /// <summary>
        /// Delete entity based on predicate filter expression
        /// </summary>
        public int DeleteMany<T>(Expression<Func<T, bool>> predicate, string collectionName = null)
        {
            return _db.GetCollection<T>(collectionName).DeleteMany(predicate);
        }

        #endregion

        #region Query

        /// <summary>
        /// Returns new instance of LiteQueryable that provides all method to query any entity inside collection. Use fluent API to apply filter/includes an than run any execute command, like ToList() or First()
        /// </summary>
        public ILiteQueryable<T> Query<T>(string collectionName = null)
        {
            return _db.GetCollection<T>(collectionName).Query();
        }

        #endregion

        #region Shortcuts

        /// <summary>
        /// Search for a single instance of T by Id. Shortcut from Query.SingleById
        /// </summary>
        public T SingleById<T>(BsonValue id, string collectionName = null)
        {
            return _db.GetCollection<T>(collectionName).Query()
                .Where("_id = @0", id)
                .Single();
        }

        /// <summary>
        /// Execute Query[T].Where(predicate).ToList();
        /// </summary>
        public List<T> Fetch<T>(BsonExpression predicate, string collectionName = null)
        {
            return this.Query<T>(collectionName)
                .Where(predicate)
                .ToList();
        }

        /// <summary>
        /// Execute Query[T].Where(predicate).ToList();
        /// </summary>
        public List<T> Fetch<T>(Expression<Func<T, bool>> predicate, string collectionName = null)
        {
            return this.Query<T>(collectionName)
                .Where(predicate)
                .ToList();
        }

        /// <summary>
        /// Execute Query[T].Where(predicate).First();
        /// </summary>
        public T First<T>(BsonExpression predicate, string collectionName = null)
        {
            return this.Query<T>(collectionName)
                .Where(predicate)
                .First();
        }

        /// <summary>
        /// Execute Query[T].Where(predicate).First();
        /// </summary>
        public T First<T>(Expression<Func<T, bool>> predicate, string collectionName = null)
        {
            return this.Query<T>(collectionName)
                .Where(predicate)
                .First();
        }

        /// <summary>
        /// Execute Query[T].Where(predicate).FirstOrDefault();
        /// </summary>
        public T FirstOrDefault<T>(BsonExpression predicate, string collectionName = null)
        {
            return this.Query<T>(collectionName)
                .Where(predicate)
                .FirstOrDefault();
        }

        /// <summary>
        /// Execute Query[T].Where(predicate).FirstOrDefault();
        /// </summary>
        public T FirstOrDefault<T>(Expression<Func<T, bool>> predicate, string collectionName = null)
        {
            return this.Query<T>(collectionName)
                .Where(predicate)
                .FirstOrDefault();
        }

        /// <summary>
        /// Execute Query[T].Where(predicate).Single();
        /// </summary>
        public T Single<T>(BsonExpression predicate, string collectionName = null)
        {
            return this.Query<T>(collectionName)
                .Where(predicate)
                .Single();
        }

        /// <summary>
        /// Execute Query[T].Where(predicate).Single();
        /// </summary>
        public T Single<T>(Expression<Func<T, bool>> predicate, string collectionName = null)
        {
            return this.Query<T>(collectionName)
                .Where(predicate)
                .Single();
        }

        /// <summary>
        /// Execute Query[T].Where(predicate).SingleOrDefault();
        /// </summary>
        public T SingleOrDefault<T>(BsonExpression predicate, string collectionName = null)
        {
            return this.Query<T>(collectionName)
                .Where(predicate)
                .SingleOrDefault();
        }

        /// <summary>
        /// Execute Query[T].Where(predicate).SingleOrDefault();
        /// </summary>
        public T SingleOrDefault<T>(Expression<Func<T, bool>> predicate, string collectionName = null)
        {
            return this.Query<T>(collectionName)
                .Where(predicate)
                .SingleOrDefault();
        }

        #endregion

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}