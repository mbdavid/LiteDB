namespace LiteDB;

internal class MakeArrayBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Array;

    internal override IEnumerable<BsonExpression> Children => this.Items;

    public IEnumerable<BsonExpression> Items { get; }

    public MakeArrayBsonExpression(IEnumerable<BsonExpression> items)
    {
        this.Items = items;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        return new BsonArray(this.Items.Select(x => x.Execute(context)));
    }

    public override bool Equals(BsonExpression expr) =>
        expr is MakeArrayBsonExpression other &&
        other.Items.SequenceEqual(this.Items);

    public override int GetHashCode() => this.Items.GetHashCode();

    public override string ToString()
    {
        return "[" + String.Join(",", this.Items.Select(x => x.ToString())) + "]";  
    }
}
