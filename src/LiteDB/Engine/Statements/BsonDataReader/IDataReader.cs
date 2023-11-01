namespace LiteDB;

public interface IDataReader : IDisposable
{
    BsonValue this[string field] { get; } 
    string Collection { get; }
    BsonValue Current { get; }
    ValueTask<bool> ReadAsync();
}