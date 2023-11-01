namespace LiteDB;

internal class FilterBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Map;

    internal override IEnumerable<BsonExpression> Children => new[] { this.Source, this.Selector };

    public BsonExpression Source { get; }

    public BsonExpression Selector { get; }

    public FilterBsonExpression(BsonExpression source, BsonExpression selector)
    {
        this.Source = source;
        this.Selector = selector;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        IEnumerable<BsonValue> source()
        {
            var src = this.Source.Execute(context);

            if (!src.IsArray) yield break;

            foreach(var item in src.AsArray)
            {
                context.Current = item;

                var value = this.Selector.Execute(context);

                if (value.IsBoolean && value.AsBoolean)
                {
                    yield return item;
                }

                context.Current = context.Root;
            }
        };

        return new BsonArray(source());
    }

    public override bool Equals(BsonExpression expr) =>
        expr is FilterBsonExpression other &&
        other.Source.Equals(this.Source) &&
        other.Selector.Equals(this.Selector);

    public override int GetHashCode() => HashCode.Combine(this.Source, this.Selector);

    public override string ToString()
    {
        return this.Source.ToString() + "[" + this.Selector.ToString() + "]";
    }
}
