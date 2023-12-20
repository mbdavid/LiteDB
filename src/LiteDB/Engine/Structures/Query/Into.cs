namespace LiteDB.Engine;

public struct Into : IIsEmpty
{
    public readonly static Into Empty = new ((IDocumentSource)null, BsonAutoId.ObjectId);

    internal readonly IDocumentSource? Source;
    public readonly BsonAutoId AutoId;

    public Into(string source, BsonAutoId autoId)
    {
        this.Source = SqlParser.ParseDocumentStore(new Tokenizer(source));
        this.AutoId = autoId;
    }

    internal Into(IDocumentSource? source, BsonAutoId autoId)
    {
        this.Source = source;
        this.AutoId = autoId;
    }

    public bool IsEmpty => this.Source is null;

}