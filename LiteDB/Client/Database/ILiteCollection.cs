using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LiteDB
{
    public interface ILiteCollection<T>
    {
        EntityMapper EntityMapper { get; }
        string Name { get; }
        int Count();
        int Count(BsonExpression predicate);
        int Count(Expression<Func<T, bool>> predicate);
        int Count(Query query);
        int Count(string predicate, BsonDocument parameters);
        int Count(string predicate, params BsonValue[] args);
        bool Delete(BsonValue id);
        int DeleteMany(BsonExpression predicate);
        int DeleteMany(Expression<Func<T, bool>> predicate);
        int DeleteMany(string predicate, BsonDocument parameters);
        int DeleteMany(string predicate, params BsonValue[] args);
        bool DropIndex(string name);
        bool EnsureIndex(BsonExpression expression, bool unique = false);
        bool EnsureIndex(string name, BsonExpression expression, bool unique = false);
        bool EnsureIndex<K>(Expression<Func<T, K>> keySelector, bool unique = false);
        bool EnsureIndex<K>(string name, Expression<Func<T, K>> keySelector, bool unique = false);
        bool Exists(BsonExpression predicate);
        bool Exists(Expression<Func<T, bool>> predicate);
        bool Exists(Query query);
        bool Exists(string predicate, BsonDocument parameters);
        bool Exists(string predicate, params BsonValue[] args);
        IEnumerable<T> Find(BsonExpression predicate, int skip = 0, int limit = int.MaxValue);
        IEnumerable<T> Find(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue);
        IEnumerable<T> Find(Query query, int skip = 0, int limit = int.MaxValue);
        IEnumerable<T> FindAll();
        T FindById(BsonValue id);
        T FindOne(BsonExpression predicate);
        T FindOne(BsonExpression predicate, params BsonValue[] args);
        T FindOne(Expression<Func<T, bool>> predicate);
        T FindOne(Query query);
        T FindOne(string predicate, BsonDocument parameters);
        ILiteCollection<T> Include(BsonExpression keySelector);
        ILiteCollection<T> Include<K>(Expression<Func<T, K>> keySelector);
        void Insert(BsonValue id, T entity);
        int Insert(IEnumerable<T> entities);
        BsonValue Insert(T entity);
        int InsertBulk(IEnumerable<T> entities, int batchSize = 5000);
        long LongCount();
        long LongCount(BsonExpression predicate);
        long LongCount(Expression<Func<T, bool>> predicate);
        long LongCount(Query query);
        long LongCount(string predicate, BsonDocument parameters);
        long LongCount(string predicate, params BsonValue[] args);
        BsonValue Max();
        BsonValue Max(BsonExpression keySelector);
        K Max<K>(Expression<Func<T, K>> keySelector);
        BsonValue Min();
        BsonValue Min(BsonExpression keySelector);
        K Min<K>(Expression<Func<T, K>> keySelector);
        ILiteQueryable<T> Query();
        bool Update(BsonValue id, T entity);
        int Update(IEnumerable<T> entities);
        bool Update(T entity);
        int UpdateMany(BsonExpression transform, BsonExpression predicate);
        int UpdateMany(Expression<Func<T, T>> extend, Expression<Func<T, bool>> predicate);
        bool Upsert(BsonValue id, T entity);
        int Upsert(IEnumerable<T> entities);
        bool Upsert(T entity);
    }
}