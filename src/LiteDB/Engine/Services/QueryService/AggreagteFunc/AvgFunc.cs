namespace LiteDB.Engine;

public class AvgFunc : IAggregateFunc
{
    private readonly BsonExpression _expr;

    private int _count = 0;
    private int _sumInt = 0;
    private long _sumLong = 0L;
    private double _sumDouble = 0d;
    private decimal _sumDecimal = 0m;

    public AvgFunc(BsonExpression expr)
    {
        _expr = expr;
    }

    public BsonExpression Expression => _expr;

    public void Iterate(BsonValue key, BsonDocument document, Collation collation)
    {
        var result = _expr.Execute(document, null, collation);

        _count++;

        if (result is BsonInt32 int32) _sumInt = unchecked(_sumInt + int32);
        else if (result is BsonInt64 int64) _sumLong = unchecked(_sumLong + int64);
        else if (result is BsonDouble double64) _sumDouble = unchecked(_sumDouble + double64);
        else if (result is BsonDecimal decimal128) _sumDecimal = unchecked(_sumDecimal + decimal128);
        else _count--;
    }

    public BsonValue GetResult()
    {
        if (_sumDecimal > 0) return (_sumDecimal + Convert.ToDecimal(_sumDouble) + _sumLong + _sumInt) / _count;
        if (_sumDouble > 0) return (_sumDouble + _sumLong + _sumInt) / _count;
        if (_sumLong > 0) return (_sumLong + _sumInt) / _count;
        if (_sumInt > 0) return (_sumInt) / _count;

        return 0;
    }

    public void Reset()
    {
        _count = _sumInt = 0;
        _sumLong = 0L;
        _sumDouble = 0d;
        _sumDecimal = 0m;
    }

    public override string ToString()
    {
        return $"AVG({_expr})";
    }
}