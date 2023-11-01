namespace LiteDB;

internal class MapBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Map;

    internal override IEnumerable<BsonExpression> Children => new[] { this.Source, this.Selector };

    public BsonExpression Source { get; }

    public BsonExpression Selector { get; }

    public MapBsonExpression(BsonExpression source, BsonExpression selector)
    {
        this.Source = source;
        this.Selector = selector;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        return new BsonArray(getSource(this.Source, this.Selector, context));

        static IEnumerable<BsonValue> getSource(BsonExpression source, BsonExpression selector, BsonExpressionContext context)
        {
            var src = source.Execute(context);

            if (src is BsonArray array)
            {
                foreach (var item in array)
                {
                    context.Current = item;

                    var value = selector.Execute(context);

                    yield return value;

                    context.Current = context.Root;
                }
            }
        };
    }

    public override bool Equals(BsonExpression expr) =>
        expr is MapBsonExpression other &&
        other.Source.Equals(this.Source) &&
        other.Selector.Equals(this.Selector);

    public override int GetHashCode() => HashCode.Combine(this.Source, this.Selector);

    public override string ToString()
    {
        return this.Source.ToString() + "=>" + this.Selector.ToString();
    }
}
