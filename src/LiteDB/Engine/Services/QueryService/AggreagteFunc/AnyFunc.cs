namespace LiteDB.Engine;

public class AnyFunc : IAggregateFunc
{
    private readonly BsonExpression _expr;

    private BsonValue _state = false;

    public AnyFunc(BsonExpression expr)
    {
        _expr = expr;
    }

    public BsonExpression Expression => _expr;

    public void Iterate(BsonValue key, BsonDocument document, Collation collation)
    {
        if (_state == false)
        {
            _state = true;
        }
    }

    public BsonValue GetResult()
    {
        return _state;
    }

    public void Reset()
    {
        _state = false;
    }

    public override string ToString()
    {
        return $"ANY({_expr})";
    }
}