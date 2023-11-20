namespace LiteDB.Engine;

internal class DropCollectionStatement : IEngineStatement
{
    private readonly IDocumentStore _store;

    public EngineStatementType StatementType => EngineStatementType.DropCollection;

    public DropCollectionStatement(IDocumentStore store)
    {
        _store = store;
    }

    public async ValueTask<int> ExecuteAsync(IServicesFactory factory, BsonDocument parameters)
    {
        using var _pc = PERF_COUNTER(39, nameof(ExecuteAsync), nameof(DropCollectionStatement));

        // dependency inejctions
        var masterService = factory.MasterService;
        var allocationMapService = factory.AllocationMapService;
        var monitorService = factory.MonitorService;

        // initialize store
        _store.Initialize(masterService);

        // get colID
        var colID = _store.ColID;

        // get exclusive $master
        var master = masterService.GetMaster(true);

        // create a new transaction locking colID = 255 ($master) and colID
        var transaction = await monitorService.CreateTransactionAsync(new byte[] { MASTER_COL_ID, colID });

        // get a list of pageID in this collection
        var pages = allocationMapService.GetAllPages(colID);

        // delete all pages writing on log disk
        transaction.DeletePages(pages);

        // remove collection from $master
        master.Collections.Remove(_store.Name);

        // write master collection into pages
        await masterService.WriteCollectionAsync(master, transaction);

        // write all dirty pages into disk
        await transaction.CommitAsync();

        // write 0 in all extends used by this collection
        allocationMapService.ClearExtends(colID);

        // update master document (only after commit completed)
        masterService.SetMaster(master);

        // release transaction
        monitorService.ReleaseTransaction(transaction);

        return 1;
    }

    public ValueTask<IDataReader> ExecuteReaderAsync(IServicesFactory factory, BsonDocument parameters) => throw new NotSupportedException();
}
