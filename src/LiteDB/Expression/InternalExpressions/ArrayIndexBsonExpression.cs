namespace LiteDB;

internal class ArrayIndexBsonExpression : BsonExpression
{
    public override BsonExpressionType Type => BsonExpressionType.ArrayIndex;

    internal override IEnumerable<BsonExpression> Children => new[] { this.Array, this.Index };

    public BsonExpression Array { get; }

    public BsonExpression Index { get; }

    public ArrayIndexBsonExpression(BsonExpression array, BsonExpression index)
    {
        this.Array = array;
        this.Index = index;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        var arr = this.Array.Execute(context);
        var idx = this.Index.Execute(context);

        if (!arr.IsArray || !idx.IsNumber) return BsonValue.Null;

        var index = idx.AsInt32;
        var array = arr.AsArray;

        // adding support for negative values (backward)
        var i = index < 0 ? array.Count + index : index;

        if (i >= array.Count) return BsonValue.Null;

        return array[i];
    }

    public override bool Equals(BsonExpression expr) =>
        expr is ArrayIndexBsonExpression other &&
        other.Array.Equals(this.Array) &&
        other.Index.Equals(this.Index);

    public override int GetHashCode() => HashCode.Combine(this.Array, this.Index);

    public override string ToString()
    {
        return this.Array.ToString() + "[" + this.Index.ToString() + "]";  
    }
}
