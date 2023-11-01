namespace LiteDB.Engine;

public class CountFunc : IAggregateFunc
{
    private readonly BsonExpression _expr;

    private long _state = 0;

    public CountFunc(BsonExpression expr)
    {
        _expr = expr;
    }

    public BsonExpression Expression => _expr;

    public void Iterate(BsonValue key, BsonDocument document, Collation collation)
    {
        var result = _expr.Execute(document, null, collation);

        if (!result.IsNull) _state++;
    }

    public BsonValue GetResult()
    {
        return _state < int.MaxValue ? new BsonInt32((int)_state) : new BsonInt64(_state);
    }

    public void Reset()
    {
        _state = 0;
    }

    public override string ToString()
    {
        return $"COUNT({_expr})";
    }
}