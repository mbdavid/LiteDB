namespace LiteDB;

internal class MakeDocumentBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Document;

    internal override IEnumerable<BsonExpression> Children => this.Values.Values;

    public IDictionary<string, BsonExpression> Values { get; }

    public MakeDocumentBsonExpression(IDictionary<string, BsonExpression> values)
    {
        this.Values = values;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        return new BsonDocument(this.Values.ToDictionary(x => x.Key, x => x.Value.Execute(context)));
    }

    public override bool Equals(BsonExpression expr) =>
        expr is MakeDocumentBsonExpression other &&
        other.Values.Keys.SequenceEqual(this.Values.Keys) &&
        other.Values.Values.SequenceEqual(this.Values.Values);

    public override int GetHashCode() => this.Values.GetHashCode();

    public override string ToString()
    {
        return "{" + String.Join(",", this.Values.Select(x => 
            (x.Key.IsWord() ? x.Key : $"\"{x.Key}\"") + ":" +
            x.Value.ToString())) + "}";
    }
}
