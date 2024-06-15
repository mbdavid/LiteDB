namespace LiteDB.Engine;

/// <summary>
///     Represent an OrderBy definition
/// </summary>
internal class OrderBy
{
    public BsonExpression Expression { get; }

    public int Order { get; set; }

    public OrderBy(BsonExpression expression, int order)
    {
        Expression = expression;
        Order = order;
    }
}