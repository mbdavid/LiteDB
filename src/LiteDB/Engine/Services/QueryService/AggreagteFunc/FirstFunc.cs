namespace LiteDB.Engine;

public class FirstFunc : IAggregateFunc
{
    private readonly BsonExpression _expr;

    private BsonValue _state = BsonValue.MinValue;

    public FirstFunc(BsonExpression expr)
    {
        _expr = expr;
    }

    public BsonExpression Expression => _expr;

    public void Iterate(BsonValue key, BsonDocument document, Collation collation)
    {
        if (_state.IsMinValue)
        {
            var result = _expr.Execute(document, null, collation);

            _state = result;
        }
    }

    public BsonValue GetResult()
    {
        return _state.IsMinValue ? BsonValue.Null : _state;
    }

    public void Reset()
    {
        _state = BsonValue.MinValue;
    }

    public override string ToString()
    {
        return $"FIRST({_expr})";
    }
}