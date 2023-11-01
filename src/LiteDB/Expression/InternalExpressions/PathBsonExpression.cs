namespace LiteDB;

internal class PathBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.Path;

    internal override IEnumerable<BsonExpression> Children => new[] { this.Source };

    public string Field { get; }

    public BsonExpression Source { get; }

    public PathBsonExpression(BsonExpression source, string field)
    {
        this.Field = field;
        this.Source = source;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        var source = this.Source.Execute(context);

        if (this.Field == null) return source;

        if (source.IsDocument)
        {
            // return document field (or null if not exists)
            return source.AsDocument[this.Field];
        }
        else if (source.IsArray)
        {
            // returns document fields inside array (only for sub documents)
            // ex: $.Name - where $ = [{Name:.., Age:..},{Name:..}] => [Name0, Name1, ...]
            return new BsonArray(source.AsArray.Select(x => x.IsDocument ? x.AsDocument[this.Field] : BsonValue.Null));
        }
        else
        {
            return BsonValue.Null;
        }
    }

    public override bool Equals(BsonExpression expr) =>
        expr is PathBsonExpression other &&
        other.Source.Equals(this.Source) &&
        other.Field == this.Field;

    public override int GetHashCode() => HashCode.Combine(this.Source, this.Field);

    public override string ToString()
    {
        var field = this.Field.IsWord() ?
            this.Field :
            "[\"" + this.Field + "\"]";

        return this.Source.ToString() + "." + field;
    }
}
