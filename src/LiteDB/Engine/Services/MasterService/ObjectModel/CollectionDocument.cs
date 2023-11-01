namespace LiteDB.Engine;

internal class CollectionDocument
{
    public required byte ColID { get; init; }
    public required string Name { get; set; } // can be changed in RenameCollection
    public required List<IndexDocument> Indexes { get; init; }

    public IndexDocument PK => this.Indexes[0];

    public CollectionDocument()
    {
    }

    /// <summary>
    /// Clone object instance constructor
    /// </summary>
    public CollectionDocument(CollectionDocument other)
    {
        this.ColID = other.ColID;
        this.Name = other.Name;
        this.Indexes = new List<IndexDocument>(other.Indexes);
    }
}

