namespace LiteDB;

internal class EmptyBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Empty;

    public EmptyBsonExpression()
    {
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        throw new NotSupportedException();
    }

    public override bool Equals(BsonExpression expr) => expr is EmptyBsonExpression;

    public override int GetHashCode() => this.Type.GetHashCode();

    public override string ToString()
    {
        return "<EMPTY>";
    }
}
