namespace LiteDB.Engine;

public class MinFunc : IAggregateFunc
{
    private readonly BsonExpression _expr;

    private BsonValue _state = BsonValue.MaxValue;

    public MinFunc(BsonExpression expr)
    {
        _expr = expr;
    }

    public BsonExpression Expression => _expr;

    public void Iterate(BsonValue key, BsonDocument document, Collation collation)
    {
        var result = _expr.Execute(document, null, collation);

        if (result.CompareTo(_state) <= 0)
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
        _state = BsonValue.MaxValue;
    }

    public override string ToString()
    {
        return $"MIN({_expr})";
    }
}