namespace LiteDB.Engine;

internal class MasterDocument
{

    /// <summary>
    /// A dictionary with all collection indexed by collection name
    /// </summary>
    public Dictionary<string, CollectionDocument> Collections { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initial master document
    /// </summary>
    public MasterDocument()
    {
    }

    /// <summary>
    /// Clone object instance constructor
    /// </summary>
    public MasterDocument(MasterDocument other)
    {
        this.Collections = new(other.Collections);
    }

    public override string ToString()
    {
        return Dump.Object(new { cols = Dump.Array(Collections) });
    }
}

