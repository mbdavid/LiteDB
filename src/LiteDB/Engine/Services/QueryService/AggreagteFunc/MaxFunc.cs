namespace LiteDB.Engine;

public class MaxFunc : IAggregateFunc
{
    private readonly BsonExpression _expr;

    private BsonValue _state = BsonValue.MinValue;

    public MaxFunc(BsonExpression expr)
    {
        _expr = expr;
    }

    public BsonExpression Expression => _expr;

    public void Iterate(BsonValue key, BsonDocument document, Collation collation)
    {
        var result = _expr.Execute(document, null, collation);

        if (result.CompareTo(_state) >= 0)
        {
            _state = result;
        }
    }

    public BsonValue GetResult()
    {
        return _state;
    }

    public void Reset()
    {
        _state = BsonValue.MinValue;
    }

    public override string ToString()
    {
        return $"MAX({_expr})";
    }
}