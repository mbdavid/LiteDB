namespace LiteDB.Engine;

public readonly struct BsonReadResult : IIsEmpty
{
    public static readonly BsonReadResult Empty = new();

    private readonly BsonValue? _value;
    private readonly Exception? _exception;

    public BsonValue Value => _value!;
    public Exception Exception => _exception!;

    public bool Ok => _exception is null;
    public bool Fail => _exception is not null;

    public bool IsEmpty => _value is null;

    public BsonReadResult()
    {
        _value = null;
        _exception = null;
    }

    public BsonReadResult(BsonValue value)
    {
        _value = value;
        _exception = null;
    }

    public BsonReadResult(Exception ex)
    {
        _value = default;
        _exception = ex;
    }

    public BsonReadResult(BsonValue value, Exception ex)
    {
        _value = value;
        _exception = ex;
    }

    public static implicit operator BsonReadResult(BsonValue value) => new (value);

    public static implicit operator BsonReadResult(Exception ex) => new (ex);

    public override string ToString()
    {
        return IsEmpty ? "<EMPTY>" : Dump.Object(new { Ok, Value = Value.ToString() });
    }
}
