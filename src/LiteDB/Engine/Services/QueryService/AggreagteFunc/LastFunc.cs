namespace LiteDB.Engine;

public class LastFunc : IAggregateFunc
{
    private readonly BsonExpression _expr;

    private BsonValue _state = BsonValue.Null;

    public LastFunc(BsonExpression expr)
    {
        _expr = expr;
    }

    public BsonExpression Expression => _expr;

    public void Iterate(BsonValue key, BsonDocument document, Collation collation)
    {
        var result = _expr.Execute(document, null, collation);

        _state = result;
    }

    public BsonValue GetResult()
    {
        return _state;
    }

    public void Reset()
    {
        _state = BsonValue.Null;
    }

    public override string ToString()
    {
        return $"LAST({_expr})";
    }
}