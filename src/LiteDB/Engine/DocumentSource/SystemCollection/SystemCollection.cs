namespace LiteDB.Engine;

internal class SystemCollection : IDocumentSource
{
    private readonly string _name;
    private readonly Func<IMasterService, IReadOnlyList<(RowID, BsonDocument)>> _sourceFn;
    private IReadOnlyList<(RowID, BsonDocument)>? _source;

    public byte ColID => 0;
    public string Name => _name;

    public SystemCollection(string name, Func<IMasterService, IReadOnlyList<(RowID, BsonDocument)>> sourceFn)
    {
        _name = name;
        _sourceFn = sourceFn;
    }

    public void Initialize(IMasterService masterService)
    {
        _source = _sourceFn(masterService);
    }

    public CollectionDocument GetCollection() => throw new NotSupportedException();

    public IReadOnlyList<IndexDocument> GetIndexes() => throw new NotSupportedException();

    public (IDataService dataService, IIndexService indexService) GetServices(IServicesFactory factory, ITransaction transaction)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<(RowID, BsonDocument source)> GetSource() => _source!;

    public void Dispose()
    {
    }
}
