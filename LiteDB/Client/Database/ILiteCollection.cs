using System;
using System.Linq.Expressions;

namespace LiteDB
{
    public interface ILiteCollection<T>
    {
        string Name { get; }

        int Count();
        int Count(BsonExpression predicate);
        int Count(string predicate, BsonDocument parameters);
        int Count(string predicate, params BsonValue[] args);
        int Count(Expression<Func<T, bool>> predicate);
        int Count(Query query);
        bool Delete(BsonValue id);
        int DeleteMany(BsonExpression predicate);
        int DeleteMany(string predicate, BsonDocument parameters);
        int DeleteMany(string predicate, params BsonValue[] args);
        int DeleteMany(Expression<Func<T, bool>> predicate);
        bool DropIndex(string name);
        bool EnsureIndex(string name, BsonExpression expression, bool unique = false);
        bool EnsureIndex(BsonExpression expression, bool unique = false);
        bool EnsureIndex<K>(Expression<Func<T, K>> keySelector, bool unique = false);
        bool EnsureIndex<K>(string name, Expression<Func<T, K>> keySelector, bool unique = false);
        bool Exists(BsonExpression predicate);
        bool Exists(string predicate, BsonDocument parameters);
        bool Exists(string predicate, params BsonValue[] args);
        bool Exists(Expression<Func<T, bool>> predicate);
        bool Exists(Query query);
        System.Collections.Generic.IEnumerable<T> Find(BsonExpression predicate, int skip = 0, int limit = int.MaxValue);
        System.Collections.Generic.IEnumerable<T> Find(Query query, int skip = 0, int limit = int.MaxValue);
        System.Collections.Generic.IEnumerable<T> Find(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue);
        System.Collections.Generic.IEnumerable<T> FindAll();
        T FindById(BsonValue id);
        T FindOne(BsonExpression predicate);
        T FindOne(string predicate, BsonDocument parameters);
        T FindOne(BsonExpression predicate, params BsonValue[] args);
        T FindOne(Expression<Func<T, bool>> predicate);
        T FindOne(Query query);
        ILiteCollection<T> Include<K>(Expression<Func<T, K>> keySelector);
        ILiteCollection<T> Include(BsonExpression keySelector);
        BsonValue Insert(T document);
        void Insert(BsonValue id, T document);
        int Insert(System.Collections.Generic.IEnumerable<T> docs);
        int InsertBulk(System.Collections.Generic.IEnumerable<T> docs, int batchSize = 5000);
        long LongCount();
        long LongCount(BsonExpression predicate);
        long LongCount(string predicate, BsonDocument parameters);
        long LongCount(string predicate, params BsonValue[] args);
        long LongCount(Expression<Func<T, bool>> predicate);
        long LongCount(Query query);
        BsonValue Max(BsonExpression keySelector);
        BsonValue Max();
        K Max<K>(Expression<Func<T, K>> keySelector);
        BsonValue Min(BsonExpression keySelector);
        BsonValue Min();
        K Min<K>(Expression<Func<T, K>> keySelector);
        ILiteQueryable<T> Query();
        bool Update(T document);
        bool Update(BsonValue id, T document);
        int Update(System.Collections.Generic.IEnumerable<T> documents);
        int UpdateMany(BsonExpression transform, BsonExpression predicate);
        int UpdateMany(Expression<Func<T, T>> extend, Expression<Func<T, bool>> predicate);
        bool Upsert(T document);
        int Upsert(System.Collections.Generic.IEnumerable<T> documents);
        bool Upsert(BsonValue id, T document);
    }
}