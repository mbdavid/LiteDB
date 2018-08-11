using System;

namespace LiteDB
{
    public interface IBsonDataReader : IDisposable
    {
        BsonValue this[string field] { get; }

        string Collection { get; }
        BsonValue Current { get; }
        bool HasValues { get; }

        event Func<IBsonDataReader> FetchNextResult;

        bool NextResult();
        bool Read();
    }
}