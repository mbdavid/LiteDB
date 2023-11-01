namespace LiteDB.Engine;

public readonly struct SelectField
{
    private static Dictionary<string, Func<BsonExpression, IAggregateFunc>> _aggregateMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        ["COUNT"] = (e) => new CountFunc(e),
        ["MIN"] = (e) => new MinFunc(e),
        ["MAX"] = (e) => new MaxFunc(e),
        ["FIRST"] = (e) => new FirstFunc(e),
        ["LAST"] = (e) => new LastFunc(e),
        ["AVG"] = (e) => new AvgFunc(e),
        ["SUM"] = (e) => new SumFunc(e),
        ["ANY"] = (e) => new AnyFunc(e),
        ["ARRAY"] = (e) => new ArrayFunc(e)
    };

    public readonly string Name;
    public readonly bool Hidden;
    public readonly BsonExpression Expression;

    public SelectField(string name, bool hidden, BsonExpression expression)
    {
        this.Name = name;
        this.Hidden = hidden;
        this.Expression = expression;
    }

    public bool IsAggregate =>
        this.Expression is CallBsonExpression call &&
         _aggregateMethods.ContainsKey(call.Method.Name);

    public IAggregateFunc CreateAggregateFunc()
    {
        var call = this.Expression as CallBsonExpression;

        var fn = _aggregateMethods[call!.Method.Name];

        // get first parameter from method call
        var expr = call.Children.FirstOrDefault()!;

        return fn(expr);
    }
}
