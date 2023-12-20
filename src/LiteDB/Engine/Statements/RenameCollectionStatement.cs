namespace LiteDB.Engine;

internal class RenameCollectionStatement : IEngineStatement
{
    private readonly string _oldName;
    private readonly string _newName;

    public EngineStatementType StatementType => EngineStatementType.RenameCollection;

    public RenameCollectionStatement(string oldName, string newName)
    {
        _oldName = oldName;
        _newName = newName;
    }

    public async ValueTask<int> ExecuteAsync(IServicesFactory factory, BsonDocument parameters)
    {
        using var _pc = PERF_COUNTER(57, nameof(ExecuteAsync), nameof(RenameCollectionStatement));

        // dependency inejctions
        var masterService = factory.MasterService;
        var monitorService = factory.MonitorService;

        // get exclusive $master
        var master = masterService.GetMaster(true);

        if (!master.Collections.TryGetValue(_oldName, out var collection)) throw ERR($"Collection {_oldName} doesn't exists");

        // test if new name already exists
        if (master.Collections.ContainsKey(_newName)) throw ERR($"Collection {_newName} already exists");

        // create a new transaction locking colID = 255 ($master) and colID
        var transaction = await monitorService.CreateTransactionAsync([MASTER_COL_ID, collection.ColID]);

        // remove collection from $master
        master.Collections.Remove(_oldName);

        // update collection name
        collection.Name = _newName;

        // re-insert with new name
        master.Collections.Add(_newName, collection);

        // write master collection into pages
        masterService.WriteCollection(master, transaction);

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
