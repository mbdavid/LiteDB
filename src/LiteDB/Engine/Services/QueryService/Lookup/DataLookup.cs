namespace LiteDB.Engine;

internal class DataLookup : IDocumentLookup
{
    private readonly string[] _fields;

    public DataLookup(string[] fields)
    {
        _fields = fields;
    }

    public BsonDocument Load(PipeValue key, PipeContext context)
    {
        var result = context.DataService.ReadDocument(key.DataBlockID, _fields);

        if (result.Fail) throw result.Exception;

        return result.Value.AsDocument;
    }

    public override string ToString()
    {
        return $"DATABLOCK {(_fields.Length == 0 ? "FULL DOCUMENT" : "FIELDS " + string.Join(", ", _fields))}";
    }
}