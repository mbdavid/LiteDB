namespace LiteDB;

/// <summary>
/// Implement a data reader for a single value
/// </summary>
public class BsonScalarReader : IDataReader
{
    private readonly string _collection;
    private readonly BsonValue _value;
    private int _current = -1;

    /// <summary>
    /// Initialize data reader with created cursor
    /// </summary>
    internal BsonScalarReader(string collection, BsonValue value)
    {
        _collection = collection;
        _value = value;
    }

    /// <summary>
    /// Return current value
    /// </summary>
    public BsonValue Current => 
        _value is BsonArray array ? 
        (_current >= 0 ? array[_current] : BsonValue.Null) :
        _value;

    /// <summary>
    /// Return collection name
    /// </summary>
    public string Collection => _collection;

    /// <summary>
    /// Move cursor to next result. Returns true if read was possible
    /// </summary>
    public ValueTask<bool> ReadAsync()
    {
        if (_current == -1)
        {
            _current = 0;

            return new ValueTask<bool>(true);
        }
        else if (_value is BsonArray array)
        {
            _current++;

            if (_current >= array.Count)
            {
                return new ValueTask<bool>(false);
            }

            return new ValueTask<bool>(true);
        }
        else
        {
            return new ValueTask<bool>(false);
        }
    }

    public BsonValue this[string field] => _value.AsDocument[field];

    public void Dispose()
    {
    }
}