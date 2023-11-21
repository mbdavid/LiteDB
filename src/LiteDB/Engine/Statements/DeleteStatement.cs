namespace LiteDB.Engine;

internal class DeleteStatement : IEngineStatement
{
    private readonly IDocumentStore _store;
    private readonly BsonExpression _whereExpr;

    public EngineStatementType StatementType => EngineStatementType.Delete;

    public DeleteStatement(IDocumentStore store, BsonExpression whereExpr)
    {
        _store = store;
        _whereExpr = whereExpr;
    }

    public async ValueTask<int> ExecuteAsync(IServicesFactory factory, BsonDocument parameters)
    {
        using var _pc = PERF_COUNTER(140, nameof(ExecuteAsync), nameof(DeleteStatement));

        // dependency injection
        var masterService = factory.MasterService;
        var monitorService = factory.MonitorService;

        // initialize document store before
        _store.Initialize(masterService);

        // create a new transaction locking colID
        var transaction = await monitorService.CreateTransactionAsync([_store.ColID]);

        // get data/index services
        var (dataService, indexService) = _store.GetServices(factory, transaction);
        var context = new PipeContext(dataService, indexService, parameters);

        var count = 0;

        // get all pkIndexNodeID + dataBlockID
        var allNodes = this.GetDeleteEnumerable(factory, transaction, parameters);

        await foreach(var node in allNodes)
        {
            // get value before DeleteAsync
            var dataBlockID = node.DataBlockID;

            // delete all index nodes starting from PK
            await indexService.DeleteAllAsync(node.IndexNodeID);

            // delete document
            await dataService.DeleteDocumentAsync(dataBlockID);

            count++;

            // do a safepoint after insert each document
            if (monitorService.Safepoint(transaction))
            {
                await transaction.SafepointAsync();
            }
        }

        // write all dirty pages into disk
        await transaction.CommitAsync();

        // release transaction
        monitorService.ReleaseTransaction(transaction);

        return count;
    }

    private async IAsyncEnumerable<PipeValue> GetDeleteEnumerable(IServicesFactory factory, ITransaction transaction, BsonDocument parameters)
    {
        var query = new Query 
        { 
            Collection = _store.Name, 
            Select = SelectFields.Id, 
            Where = _whereExpr
        };

        var optimizator = factory.CreateQueryOptimization();

        var enumerator = optimizator.ProcessQuery(query, parameters);

        var (dataService, indexService) = _store.GetServices(factory, transaction);
        var context = new PipeContext(dataService, indexService, parameters);

        var pkIndex = _store.GetIndexes()[0];

        while (true)
        {
            var result = await enumerator.MoveNextAsync(context);

            if (result.IsEmpty) break;

            if (optimizator.IndexName == "_id")
            {
                yield return result;
            }
            else
            {
                var id = result.Value["_id"];

                // get PK index node based on _id value
                var node = await context.IndexService.FindAsync(pkIndex, id, false, Query.Ascending);

                yield return new PipeValue(node.IndexNodeID, node.DataBlockID, id);
            }
        }
    }

    public ValueTask<IDataReader> ExecuteReaderAsync(IServicesFactory factory, BsonDocument parameters) => throw new NotSupportedException();
}
