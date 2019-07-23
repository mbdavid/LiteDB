using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LiteDB
{
    public interface ILiteQueryable<T> : ILiteQueryableResult<T>
    {
        ILiteQueryable<T> Include(BsonExpression path);
        ILiteQueryable<T> Include(List<BsonExpression> paths);
        ILiteQueryable<T> Include<K>(Expression<Func<T, K>> path);

        ILiteQueryable<T> Where(BsonExpression predicate);
        ILiteQueryable<T> Where(string predicate, BsonDocument parameters);
        ILiteQueryable<T> Where(string predicate, params BsonValue[] args);
        ILiteQueryable<T> Where(Expression<Func<T, bool>> predicate);

        ILiteQueryable<T> OrderBy(BsonExpression keySelector, int order = 1);
        ILiteQueryable<T> OrderBy<K>(Expression<Func<T, K>> keySelector, int order = 1);
        ILiteQueryable<T> OrderByDescending(BsonExpression keySelector);
        ILiteQueryable<T> OrderByDescending<K>(Expression<Func<T, K>> keySelector);

        ILiteQueryable<T> Limit(int limit);
        ILiteQueryable<T> Skip(int offset);
        ILiteQueryable<T> Offset(int offset);
        ILiteQueryable<T> ForUpdate();

        ILiteQueryable<T> GroupBy(BsonExpression keySelector);
        ILiteQueryable<T> GroupBy<K>(Expression<Func<T, K>> keySelector);

        ILiteQueryable<T> Having(BsonExpression predicate);
        ILiteQueryable<T> Having(Expression<Func<IEnumerable<T>, bool>> keySelector);

        ILiteQueryableResult<BsonDocument> Select(BsonExpression selector);
        ILiteQueryableResult<K> Select<K>(Expression<Func<T, K>> selector);
        ILiteQueryableResult<K> SelectAll<K>(Expression<Func<IEnumerable<T>, K>> selector);
    }

    public interface ILiteQueryableResult<T>
    {
        BsonDocument GetPlan();
        IBsonDataReader ExecuteReader();
        IEnumerable<BsonDocument> ToDocuments();
        IEnumerable<T> ToEnumerable();
        List<T> ToList();
        T[] ToArray();

        int Into(string newCollection, BsonAutoId autoId = BsonAutoId.ObjectId);

        T First();
        T FirstOrDefault();
        T Single();
        T SingleOrDefault();

        int Count();
        long LongCount();
        bool Exists();
    }
}