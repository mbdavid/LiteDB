namespace LiteDB.Engine;

internal interface IDocumentSource : IDisposable
{
    byte ColID { get; }

    string Name { get; }

    /// <summary>
    /// Should be call this method just after enter in execution on statement. Will load internal collection
    /// </summary>
    void Initialize(IMasterService masterService);

    CollectionDocument GetCollection();

    IReadOnlyList<IndexDocument> GetIndexes();

    (IDataService dataService, IIndexService indexService) GetServices(IServicesFactory factory, ITransaction transaction);



    // Dipose will be run in statement dispose
}
