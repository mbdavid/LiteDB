using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LiteDB
{
    public interface ILiteRepository : IDisposable
    {
        ILiteDatabase Database { get; }
        bool Delete<T>(BsonValue id, string collectionName = null);
        int DeleteMany<T>(BsonExpression predicate, string collectionName = null);
        int DeleteMany<T>(Expression<Func<T, bool>> predicate, string collectionName = null);
        bool EnsureIndex<T, K>(Expression<Func<T, K>> keySelector, bool unique = false, string collectionName = null);
        bool EnsureIndex<T, K>(string name, Expression<Func<T, K>> keySelector, bool unique = false, string collectionName = null);
        bool EnsureIndex<T>(BsonExpression expression, bool unique = false, string collectionName = null);
        bool EnsureIndex<T>(string name, BsonExpression expression, bool unique = false, string collectionName = null);
        List<T> Fetch<T>(BsonExpression predicate, string collectionName = null);
        List<T> Fetch<T>(Expression<Func<T, bool>> predicate, string collectionName = null);
        T First<T>(BsonExpression predicate, string collectionName = null);
        T First<T>(Expression<Func<T, bool>> predicate, string collectionName = null);
        T FirstOrDefault<T>(BsonExpression predicate, string collectionName = null);
        T FirstOrDefault<T>(Expression<Func<T, bool>> predicate, string collectionName = null);
        int Insert<T>(IEnumerable<T> entities, string collectionName = null);
        void Insert<T>(T entity, string collectionName = null);
        ILiteQueryable<T> Query<T>(string collectionName = null);
        T Single<T>(BsonExpression predicate, string collectionName = null);
        T Single<T>(Expression<Func<T, bool>> predicate, string collectionName = null);
        T SingleById<T>(BsonValue id, string collectionName = null);
        T SingleOrDefault<T>(BsonExpression predicate, string collectionName = null);
        T SingleOrDefault<T>(Expression<Func<T, bool>> predicate, string collectionName = null);
        int Update<T>(IEnumerable<T> entities, string collectionName = null);
        bool Update<T>(T entity, string collectionName = null);
        int Upsert<T>(IEnumerable<T> entities, string collectionName = null);
        bool Upsert<T>(T entity, string collectionName = null);
    }
}