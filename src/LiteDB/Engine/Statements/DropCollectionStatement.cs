namespace LiteDB.Engine;

internal class DropCollectionStatement : IEngineStatement
{
    private readonly string _collectionName;

    public EngineStatementType StatementType => EngineStatementType.DropCollection;

    public DropCollectionStatement(string collectionName)
    {
        _collectionName = collectionName;
    }

    public async ValueTask<int> ExecuteAsync(IServicesFactory factory, BsonDocument parameters)
    {
        using var _pc = PERF_COUNTER(39, nameof(ExecuteAsync), nameof(DropCollectionStatement));

        // dependency inejctions
        var masterService = factory.MasterService;
        var allocationMapService = factory.AllocationMapService;
        var monitorService = factory.MonitorService;

        // get exclusive $master
        var master = masterService.GetMaster(true);

        if (!master.Collections.TryGetValue(_collectionName, out var collection)) throw ERR($"Collection {_collectionName} not found");

        var colID = collection.ColID;

        // create a new transaction locking colID = 255 ($master) and colID
        var transaction = await monitorService.CreateTransactionAsync([MASTER_COL_ID, colID]);

        // get a list of pageID in this collection
        var pages = allocationMapService.GetAllPages(colID);

        // delete all pages writing on log disk
        transaction.DeletePages(pages);

        // remove collection from $master
        master.Collections.Remove(_collectionName);

        // write master collection into pages
        masterService.WriteCollection(master, transaction);

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
