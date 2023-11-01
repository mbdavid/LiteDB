namespace LiteDB;

internal class ConstantBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Constant;

    public BsonValue Value { get; }

    public ConstantBsonExpression(BsonValue value)
    {
        this.Value = value;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        return this.Value;
    }

    public override bool Equals(BsonExpression expr) =>
        expr is ConstantBsonExpression other &&
        other.Value.Equals(this.Value);

    public override int GetHashCode() => this.Value.GetHashCode();

    public override string ToString()
    {
        return this.Value.ToString();
    }
}
