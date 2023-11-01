namespace LiteDB.Engine;

/// <summary>
/// Document store factory to cache results
/// </summary>
[AutoInterface]
internal class DocumentStoreFactory : IDocumentStoreFactory
{
    public IDocumentStore GetUserCollection(string name)
    {
        //TODO: como reaproveitar? cache direto pode não ser uma boa
        return new UserCollectionStore(name);
    }

    public IDocumentStore GetVirtualCollection(string name, BsonDocument parameters)
    {
        //TODO: vai conter SWITCH para decidir
        throw new NotImplementedException();
    }
}
