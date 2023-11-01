namespace LiteDB.Engine;

internal class UserCollectionStore : IDocumentStore
{
    private readonly string _name;
    private CollectionDocument? _collection; // will load on Initialize()

    public byte ColID => _collection?.ColID ?? 0;
    public string Name => _name;
    public IReadOnlyList<IndexDocument> Indexes => _collection?.Indexes ?? (IReadOnlyList<IndexDocument>)Array.Empty<IndexDocument>();


    public UserCollectionStore(string name)
    {
        _name = name;
    }

    public void Initialize(IMasterService masterService)
    {
        var master = masterService.GetMaster(false);

        if (master.Collections.TryGetValue(_name, out var collection))
        {
            _collection = collection;
        }
        else
        {
            throw ERR($"Collection {_name} does not exist");
        }
    }

    public IReadOnlyList<IndexDocument> GetIndexes() => 
        _collection!.Indexes;

    public (IDataService dataService, IIndexService indexService) GetServices(IServicesFactory factory, ITransaction transaction) =>
        (factory.CreateDataService(transaction), factory.CreateIndexService(transaction));

    public IPipeEnumerator GetPipeEnumerator(BsonExpression expression)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        // in user collection, there is nothing to dispose
        _collection = null;
    }
}
