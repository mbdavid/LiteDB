namespace LiteDB;

internal class BinaryBsonExpression : BsonExpression
{
    public override BsonExpressionType Type { get; }

    internal override IEnumerable<BsonExpression> Children => new[] { this.Left, this.Right };

    public BsonExpression Left { get; }

    public BsonExpression Right { get; }

    public BinaryBsonExpression(BsonExpressionType type, BsonExpression left, BsonExpression right)
    {
        this.Type = type;
        this.Left = left;
        this.Right = right;
    }

    internal override BsonValue Execute(BsonExpressionContext context)
    {
        switch (this.Type)
        {
            case BsonExpressionType.Add:
                return this.Left.Execute(context) + this.Right.Execute(context);
            case BsonExpressionType.Subtract:
                return this.Left.Execute(context) - this.Right.Execute(context);
            case BsonExpressionType.Multiply:
                return this.Left.Execute(context) * this.Right.Execute(context);
            case BsonExpressionType.Divide:
                return this.Left.Execute(context) / this.Right.Execute(context);
            case BsonExpressionType.Modulo:
                return this.Left.Execute(context) % this.Right.Execute(context);

            case BsonExpressionType.Equal:
                return context.Collation.Equals(this.Left.Execute(context), this.Right.Execute(context));
            case BsonExpressionType.NotEqual:
                return !context.Collation.Equals(this.Left.Execute(context), this.Right.Execute(context));
            case BsonExpressionType.GreaterThan:
                return context.Collation.Compare(this.Left.Execute(context), this.Right.Execute(context)) > 0;
            case BsonExpressionType.GreaterThanOrEqual:
                return context.Collation.Compare(this.Left.Execute(context), this.Right.Execute(context)) >= 0;
            case BsonExpressionType.LessThan:
                return context.Collation.Compare(this.Left.Execute(context), this.Right.Execute(context)) < 0;
            case BsonExpressionType.LessThanOrEqual:
                return context.Collation.Compare(this.Left.Execute(context), this.Right.Execute(context)) <= 0;

            case BsonExpressionType.Like:
                return this.Left.Execute(context).AsString?.SqlLike(this.Right.Execute(context).AsString, context.Collation) ?? false;
            case BsonExpressionType.Between:
                var value = this.Left.Execute(context);
                var start = (this.Right as MakeArrayBsonExpression)!.Items.First().Execute(context);
                var end = (this.Right as MakeArrayBsonExpression)!.Items.Last().Execute(context);
                return value >= start && value <= end;
            case BsonExpressionType.In:
                return this.Right.Execute(context).AsArray?.Contains(this.Left.Execute(context), context.Collation) ?? false;
            case BsonExpressionType.Contains:
                return this.Left.Execute(context).AsArray?.Contains(this.Right.Execute(context)) ?? false;

            case BsonExpressionType.Or:
                var lOr = this.Left.Execute(context);
                if (lOr.IsBoolean && lOr.AsBoolean) return true;
                var rOr = this.Right.Execute(context);
                if (rOr.IsBoolean && rOr.AsBoolean) return true;
                return false;

            case BsonExpressionType.And:
                var lAnd = this.Left.Execute(context);
                if (!lAnd.IsBoolean || !lAnd.AsBoolean) return false;
                var rAnd = this.Right.Execute(context);
                if (!rAnd.IsBoolean || !rAnd.AsBoolean) return false;
                return true;
        }

        throw new InvalidOperationException("BsonExpressionType type are not valid as a BinaryBsonExpression");
    }

    public override bool Equals(BsonExpression expr) =>
        expr is BinaryBsonExpression other &&
        other.Left.Equals(this.Left) &&
        other.Right.Equals(this.Right) &&
        other.Type == this.Type;

    public override int GetHashCode() => HashCode.Combine(this.Left, this.Right, this.Type);

    public override string ToString()
    {
        switch (this.Type)
        {
            case BsonExpressionType.Add:
                return this.Left.ToString() + "+" + this.Right.ToString();
            case BsonExpressionType.Subtract:
                return this.Left.ToString() + "-" + this.Right.ToString();
            case BsonExpressionType.Multiply:
                return this.Left.ToString() + "*" + this.Right.ToString();
            case BsonExpressionType.Divide:
                return this.Left.ToString() + "/" + this.Right.ToString();
            case BsonExpressionType.Modulo:
                return this.Left.ToString() + "%" + this.Right.ToString();

            case BsonExpressionType.Equal:
                return this.Left.ToString() + "=" + this.Right.ToString();
            case BsonExpressionType.NotEqual:
                return this.Left.ToString() + "!=" + this.Right.ToString();
            case BsonExpressionType.GreaterThan:
                return this.Left.ToString() + ">" + this.Right.ToString();
            case BsonExpressionType.GreaterThanOrEqual:
                return this.Left.ToString() + ">=" + this.Right.ToString();
            case BsonExpressionType.LessThan:
                return this.Left.ToString() + "<" + this.Right.ToString();
            case BsonExpressionType.LessThanOrEqual:
                return this.Left.ToString() + "<=" + this.Right.ToString();

            case BsonExpressionType.Like:
                return this.Left.ToString() + " LIKE " + this.Right.ToString();
            case BsonExpressionType.Between:
                var values = (this.Right as MakeArrayBsonExpression)!;
                return this.Left.ToString() + " BETWEEN " + values.Items.First().ToString() + " AND " + values.Items.Last().ToString();
            case BsonExpressionType.In:
                return this.Left.ToString() + " IN " + this.Right.ToString();
            case BsonExpressionType.Contains:
                return this.Left.ToString() + " CONTAINS " + this.Right.ToString();

            case BsonExpressionType.Or:
                return this.Left.ToString() + " OR " + this.Right.ToString();
            case BsonExpressionType.And:
                return this.Left.ToString() + " AND " + this.Right.ToString();
        }

        throw new InvalidOperationException("BsonExpressionType type are not valid as a BinaryBsonExpression");
    }
}
