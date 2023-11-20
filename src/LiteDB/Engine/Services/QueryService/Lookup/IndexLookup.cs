namespace LiteDB.Engine;

internal class IndexLookup : IDocumentLookup
{
    private readonly string _field;

    public IndexLookup(string field)
    {
        _field = field;
    }

    public ValueTask<BsonDocument> LoadAsync(PipeValue key, PipeContext context)
    {
        var doc = new BsonDocument { [_field] = key.Value! };

        return new ValueTask<BsonDocument>(doc);
    }

    public override string ToString()
    {
        return $"INDEX FIELD {_field}";
    }
}