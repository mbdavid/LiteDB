namespace LiteDB.Engine;

internal interface IDocumentLookup
{
    ValueTask<BsonDocument> LoadAsync(PipeValue key, PipeContext context);
}