namespace LiteDB.Engine;

internal class RenameCollectionStatement : IEngineStatement
{
    private readonly IDocumentStore _store;
    private readonly string _name;

    public EngineStatementType StatementType => EngineStatementType.RenameCollection;

    public RenameCollectionStatement(IDocumentStore store, string newName)
    {
        _store = store;
        _name = newName;
    }

    public async ValueTask<int> ExecuteAsync(IServicesFactory factory, BsonDocument parameters)
    {
        using var _pc = PERF_COUNTER(57, nameof(ExecuteAsync), nameof(RenameCollectionStatement));

        // dependency inejctions
        var masterService = factory.MasterService;
        var monitorService = factory.MonitorService;

        // initialize store
        _store.Initialize(masterService);

        // get collection
        var collection = _store.GetCollection();

        // get exclusive $master
        var master = masterService.GetMaster(true);

        // create a new transaction locking colID = 255 ($master) and colID
        var transaction = await monitorService.CreateTransactionAsync(new byte[] { MASTER_COL_ID, _store.ColID });

        // remove collection from $master
        master.Collections.Remove(_store.Name);

        // update collection name
        collection.Name = _name;

        // re-insert with new name
        master.Collections.Add(_name, collection);

        // write master collection into pages
        masterService.WriteCollectionAsync(master, transaction);

        // write all dirty pages into disk
        await transaction.CommitAsync();

        // update master document (only after commit completed)
        masterService.SetMaster(master);

        // release transaction
        monitorService.ReleaseTransaction(transaction);

        return 1;
    }

    public ValueTask<IDataReader> ExecuteReaderAsync(IServicesFactory factory, BsonDocument parameters) => throw new NotSupportedException();
}
