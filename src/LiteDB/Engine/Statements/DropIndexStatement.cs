namespace LiteDB.Engine;

internal class DropIndexStatement : IEngineStatement
{
    private readonly IDocumentStore _store;
    private readonly string _indexName;

    public EngineStatementType StatementType => EngineStatementType.DropIndex;

    public DropIndexStatement(IDocumentStore store, string indexName)
    {
        _store = store;
        _indexName = indexName;
    }

    public async ValueTask<int> ExecuteAsync(IServicesFactory factory, BsonDocument parameters)
    {
        using var _pc = PERF_COUNTER(160, nameof(ExecuteAsync), nameof(DropIndexStatement));

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

        if (!master.Collections.TryGetValue(_store.Name, out var collection)) throw ERR($"Collection {_store.Name} not found");

        // get index
        var pkIndex = collection.Indexes[0];
        var indexDocument = collection.Indexes.FirstOrDefault(x => x.Name.Eq(_indexName));

        if (indexDocument is null) throw ERR($"Index {_indexName} not found on {_store.Name}");

        // get index slot number
        var slot = indexDocument.Slot;

        // create a new transaction locking colID = 255 ($master) and colID
        var transaction = await monitorService.CreateTransactionAsync([MASTER_COL_ID, colID]);

        // get index service from store
        var (_, indexService) = _store.GetServices(factory, transaction);

        // drop all index nodes for this slot. Scan from pk items
        await indexService.DropIndexAsync(indexDocument.Slot, pkIndex.HeadIndexNodeID, pkIndex.TailIndexNodeID);

        // remove index from collection on $master
        collection.Indexes.Remove(indexDocument);

        // write master collection into pages
        await masterService.WriteCollectionAsync(master, transaction);

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
