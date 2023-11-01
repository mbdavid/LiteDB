namespace LiteDB.Engine;

internal interface IDocumentStore : IDisposable
{
    byte ColID { get; }
    string Name { get; }

    /// <summary>
    /// Should be call this method just after enter in execution on statement. Will load internal collection
    /// </summary>
    void Initialize(IMasterService masterService);

    IReadOnlyList<IndexDocument> GetIndexes();

    (IDataService dataService, IIndexService indexService) GetServices(IServicesFactory factory, ITransaction transaction);

    IPipeEnumerator GetPipeEnumerator(BsonExpression expression);

    // Dipose will be run in statement dispose
}
