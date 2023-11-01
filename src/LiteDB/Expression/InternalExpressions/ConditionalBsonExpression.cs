namespace LiteDB;

internal class ConditionalBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Conditional;

    internal override IEnumerable<BsonExpression> Children => new BsonExpression[] { IfTest, TrueExpr, FalseExpr };

    public BsonExpression IfTest { get; }
    public BsonExpression TrueExpr { get; }
    public BsonExpression FalseExpr { get; }

    public ConditionalBsonExpression(BsonExpression ifTest, BsonExpression trueExpr, BsonExpression falseExpr)
    {
        this.IfTest = ifTest;
        this.TrueExpr = trueExpr;
        this.FalseExpr = falseExpr;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        var result = this.IfTest.Execute(context);

        if (result.IsBoolean && result.AsBoolean)
        {
            return this.TrueExpr.Execute(context);
        }
        else
        {
            return this.FalseExpr.Execute(context);
        }
    }

    public override bool Equals(BsonExpression expr) =>
        expr is ConditionalBsonExpression other &&
        other.IfTest == this.IfTest &&
        other.TrueExpr == this.TrueExpr &&
        other.FalseExpr == this.FalseExpr;

    public override int GetHashCode() => HashCode.Combine(IfTest, TrueExpr, FalseExpr);

    public override string ToString()
    {
        return this.IfTest.ToString() + "?" + this.TrueExpr.ToString() + ":" + this.FalseExpr.ToString();
    }
}
