namespace LiteDB.Engine;

internal class CreateCollectionStatement : IEngineStatement
{
    private readonly string _name;

    public EngineStatementType StatementType => EngineStatementType.CreateCollection;

    public CreateCollectionStatement(string name)
    {
        if (!name.IsWord()) throw ERR("Invalid collection name");
        if (name.StartsWith("$")) throw ERR("Invalid collection name");

        _name = name;
    }

    public async ValueTask<int> ExecuteAsync(IServicesFactory factory, BsonDocument parameters)
    {
        using var _pc = PERF_COUNTER(34, nameof(ExecuteAsync), nameof(CreateCollectionStatement));

        // dependency inejctions
        var masterService = factory.MasterService;
        var monitorService = factory.MonitorService;

        // get exclusive $master
        var master = masterService.GetMaster(true);

        // test if already exists
        if (master.Collections.ContainsKey(_name)) throw ERR($"coleção {_name} já existe");

        // get a new colID
        var colID = (byte)Enumerable.Range(1, MASTER_COL_LIMIT + 1)
            .Where(x => master.Collections.Values.Any(y => y.ColID == x) == false)
            .FirstOrDefault();

        if (colID > MASTER_COL_LIMIT) throw ERR("acima do limite");

        // create a new transaction locking colID = 255 ($master)
        var transaction = await monitorService.CreateTransactionAsync(new byte[] { MASTER_COL_ID, colID });

        // get index service
        var indexer = factory.CreateIndexService(transaction);

        // insert head/tail nodes
        var (head, tail) = indexer.CreateHeadTailNodes(colID);

        // create new collection in $master and returns a new master document
        master.Collections.Add(_name, new CollectionDocument()
        {
            ColID = colID,
            Name = _name,
            Indexes = new List<IndexDocument>
            {
                new IndexDocument
                {
                    Slot = 0,
                    Name = "_id",
                    Expression = "$._id",
                    Unique = true,
                    HeadIndexNodeID = head,
                    TailIndexNodeID = tail
                }
            }
        });

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
