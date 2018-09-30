using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LiteDB
{
    public interface ILiteQueryableWithIncludes<T>
        : ILiteQueryable<T>
    {
        ILiteQueryableWithIncludes<T> Include(BsonExpression path);
        ILiteQueryableWithIncludes<T> Include(List<BsonExpression> paths);
        ILiteQueryableWithIncludes<T> Include<K>(Expression<Func<T, K>> path);
    }

    public interface ILiteQueryable<T> :
        ILiteQueryableFiltered<T>
    {
        ILiteQueryable<T> Where(BsonExpression predicate);
        ILiteQueryable<T> Where(string predicate, BsonDocument parameters);
        ILiteQueryable<T> Where(string predicate, params BsonValue[] args);
        ILiteQueryable<T> Where(Expression<Func<T, bool>> predicate);

        ILiteQueryableFiltered<T> GroupBy(BsonExpression keySelector);
        ILiteQueryableFiltered<T> GroupBy<K>(Expression<Func<T, K>> keySelector);
    }

    public interface ILiteQueryableFiltered<T> :
        ILiteQueryableSelected<T>
    {
        ILiteQueryableSelected<BsonDocument> Select(BsonExpression selector);
        ILiteQueryableSelected<K> Select<K>(Expression<Func<T, K>> selector);
        ILiteQueryableSelected<BsonDocument> SelectAll(BsonExpression selector);
        ILiteQueryableSelected<K> SelectAll<K>(Expression<Func<T, K>> selector);
    }

    public interface ILiteQueryableSelected<T> :
        ILiteQueryableOrdered<T>
    {
        ILiteQueryableSelected<T> Having(BsonExpression predicate);
        ILiteQueryableSelected<T> Having(Expression<Func<T, bool>> predicate);

        ILiteQueryableOrdered<T> OrderBy(BsonExpression keySelector, int order = 1);
        ILiteQueryableOrdered<T> OrderBy<K>(Expression<Func<T, K>> keySelector, int order = 1);
        ILiteQueryableOrdered<T> OrderByDescending(BsonExpression keySelector);
        ILiteQueryableOrdered<T> OrderByDescending<K>(Expression<Func<T, K>> keySelector);
    }

    public interface ILiteQueryableOrdered<T> :
        ILiteQueryableResult<T>
    {
        ILiteQueryableOrdered<T> Limit(int limit);
        ILiteQueryableOrdered<T> Skip(int offset);
        ILiteQueryableOrdered<T> Offset(int offset);
        ILiteQueryableOrdered<T> ForUpdate();
    }

    public interface ILiteQueryableResult<T>
    {
        BsonDocument GetPlan();
        IBsonDataReader ExecuteReader();
        T ExecuteScalar();
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