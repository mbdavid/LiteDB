namespace LiteDB.Engine;

internal interface IDocumentLookup
{
    BsonDocument Load(PipeValue key, PipeContext context);
}