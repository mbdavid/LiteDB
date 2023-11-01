namespace LiteDB.Engine;

public struct Into : IIsEmpty
{
    public readonly static Into Empty = new ("", BsonAutoId.ObjectId);

    public readonly string Collection;
    public readonly BsonAutoId AutoId;

    public Into(string store, BsonAutoId autoId)
    {
        this.Collection = store;
        this.AutoId = autoId;
    }

    public bool IsEmpty => string.IsNullOrEmpty(Collection);

}