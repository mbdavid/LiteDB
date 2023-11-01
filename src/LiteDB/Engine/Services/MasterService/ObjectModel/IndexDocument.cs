namespace LiteDB.Engine;

internal class IndexDocument
{
    public required byte Slot { get; init; }
    public required string Name { get; init; }
    public required BsonExpression Expression { get; init; }
    public required bool Unique { get; init; }
    public required RowID HeadIndexNodeID { get; init; }
    public required RowID TailIndexNodeID { get; init; }

    public IndexDocument()
    {
    }

    /// <summary>
    /// Clone object instance constructor
    /// </summary>
    public IndexDocument(IndexDocument other)
    {
        this.Slot = other.Slot;
        this.Name = other.Name;
        this.Expression = other.Expression;
        this.Unique = other.Unique;
        this.HeadIndexNodeID = other.HeadIndexNodeID;
        this.TailIndexNodeID = other.TailIndexNodeID;
    }
}

