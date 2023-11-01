namespace LiteDB.Engine;

internal readonly struct SortItemDocument
{
    public readonly RowID DataBlockID;
    public readonly BsonValue Key;
    public readonly BsonDocument Document;

    public SortItemDocument(RowID dataBlockID, BsonValue key, BsonDocument document)
    {
        this.DataBlockID = dataBlockID;
        this.Key = key;
        this.Document = document;
    }

    public override string ToString() => Dump.Object(this);
}
