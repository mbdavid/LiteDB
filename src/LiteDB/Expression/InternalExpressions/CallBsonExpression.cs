namespace LiteDB;

internal class CallBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Call;

    internal override IEnumerable<BsonExpression> Children => this.Parameters;

    public BsonExpression[] Parameters { get; }

    public MethodInfo Method { get; }

    public bool IsVolatile { get; }

    private readonly bool _collation;

    public CallBsonExpression(MethodInfo method, BsonExpression[] parameters)
    {
        this.Method = method;
        this.Parameters = parameters;
        this.IsVolatile = method.GetCustomAttribute<VolatileAttribute>() is not null;

        _collation = method.GetParameters().FirstOrDefault()?.ParameterType == typeof(Collation);
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        // if contains collation parameter, must be fist parameter
        var values = _collation ?
            new object[] { context.Collation }.Union(this.Parameters.Select(x => x.Execute(context))) :
        this.Parameters.Select(x => x.Execute(context));

        return (BsonValue)this.Method.Invoke(null, values.ToArray());
    }

    public override bool Equals(BsonExpression expr) =>
        expr is CallBsonExpression other &&
        other.Method.Name == this.Method.Name &&
        other.Parameters.SequenceEqual(this.Parameters) &&
        other.IsVolatile == this.IsVolatile;

    public override int GetHashCode() => HashCode.Combine(this.Method, this.Parameters);

    public override string ToString()
    {
        return this.Method.Name + "(" + string.Join(",", this.Parameters.Select(x => x.ToString())) + ")";
    }
}
