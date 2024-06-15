namespace LiteDB.Engine;

/// <summary>
///     Represent an GroupBy definition (is based on OrderByDefinition)
/// </summary>
internal class GroupBy
{
    public BsonExpression Expression { get; }

    public BsonExpression Select { get; }

    public BsonExpression Having { get; }

    public GroupBy(BsonExpression expression, BsonExpression select, BsonExpression having)
    {
        Expression = expression;
        Select = select;
        Having = having;
    }
}