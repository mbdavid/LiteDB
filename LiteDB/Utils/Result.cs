namespace LiteDB;

using System;

/// <summary>
///     Implement a generic result structure with value and exception. This value can be partial value (like
///     BsonDocument/Array)
/// </summary>
internal struct Result<T>
    where T : class
{
    public T Value;
    public Exception Exception;

    public bool Ok => Exception == null;
    public bool Fail => Exception != null;

    /// <summary>
    ///     Get array result or throw exception if there is any error on read result
    /// </summary>
    public T GetValue() => Ok ? Value : throw Exception;

    public Result(T value, Exception ex = null)
    {
        Value = value;
        Exception = ex;
    }


    public static implicit operator T(Result<T> value)
    {
        return value.Value;
    }

    public static implicit operator Result<T>(T value)
    {
        return new Result<T>(value);
    }
}