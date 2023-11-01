namespace LiteDB.Engine;

public readonly struct SelectFields
{
    /// <summary>
    /// SELECT *
    /// </summary>
    public static readonly SelectFields Root = new (BsonExpression.Root);

    /// <summary>
    /// SELECT $._id
    /// </summary>
    public static readonly SelectFields Id = new(new SelectField[] { new SelectField("_id", false, BsonExpression.Id) });

    // fields
    public readonly BsonExpression SingleExpression;
    public readonly IReadOnlyList<SelectField> Fields;

    // properties

    /// <summary>
    /// Indicate this query will return root/full document. Means "SELECT *"
    /// </summary>
    public bool IsRoot => this.SingleExpression == BsonExpression.Root;

    /// <summary>
    /// Indicate this SELECT fields contains a single result (single expression document)
    /// </summary>
    public bool IsSingleExpression => Fields.Count == 0;

    /// <summary>
    /// Indicate this SELECT fields contains at least 1 aggregate expression
    /// </summary>
    public bool HasAggregate => this.Fields.Any(x => x.IsAggregate);

    public SelectFields(BsonExpression docExpr)
    {
        this.SingleExpression = docExpr;
        this.Fields = Array.Empty<SelectField>();
    }

    public SelectFields(IReadOnlyList<SelectField> fields)
    {
        this.SingleExpression = BsonExpression.Empty;
        this.Fields = fields;
    }

    public override string ToString()
    {
        return IsSingleExpression ? 
            SingleExpression.ToString()! : 
            string.Join(", ", Fields.Select(x => x.Name));
    }
}
