namespace LiteDB.Engine;

public class ArrayFunc : IAggregateFunc
{
    private readonly BsonExpression _expr;

    private BsonArray _state = new BsonArray();

    public ArrayFunc(BsonExpression expr)
    {
        _expr = expr;
    }

    public BsonExpression Expression => _expr;

    public void Iterate(BsonValue key, BsonDocument document, Collation collation)
    {
        var result = _expr.Execute(document, null, collation);

        _state.Add(result);
    }

    public BsonValue GetResult()
    {
        return _state;
    }

    public void Reset()
    {
        _state = new BsonArray();
    }

    public override string ToString()
    {
        return $"ARRAY({_expr})";
    }
}