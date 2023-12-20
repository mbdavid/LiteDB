namespace LiteDB.Engine;

internal class DeleteStatement : IEngineStatement
{
    private readonly string _collectionName;
    private readonly BsonExpression _whereExpr;

    public EngineStatementType StatementType => EngineStatementType.Delete;

    public DeleteStatement(string collectionName, BsonExpression whereExpr)
    {
        _collectionName = collectionName;
        _whereExpr = whereExpr;
    }

    public async ValueTask<int> ExecuteAsync(IServicesFactory factory, BsonDocument parameters)
    {
        using var _pc = PERF_COUNTER(71, nameof(ExecuteAsync), nameof(DeleteStatement));

        // dependency injection
        var masterService = factory.MasterService;
        var monitorService = factory.MonitorService;

        // get exclusive $master
        var master = masterService.GetMaster(false);

        if (!master.Collections.TryGetValue(_collectionName, out var collection)) throw ERR($"Collection {_collectionName} not found");

        // create a new transaction locking colID
        var transaction = await monitorService.CreateTransactionAsync([collection.ColID]);

        // get data/index services
        var pkIndex = collection.Indexes[0];
        var dataService = factory.CreateDataService(transaction);
        var indexService = factory.CreateIndexService(transaction);
        var context = new PipeContext(dataService, indexService, parameters);

        var count = 0;

        // get all pkIndexNodeID + dataBlockID
        var allNodes = this.GetDeleteEnumerable(context, pkIndex, factory, parameters)
            .ToArray(); // TODO: fix memory allocation

        foreach(var node in allNodes)
        {
            // get value before DeleteAsync
            var dataBlockID = node.DataBlockID;

            // delete all index nodes starting from PK
            indexService.DeleteAll(node.IndexNodeID);

            // delete document
            dataService.DeleteDocument(dataBlockID);

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

    private IEnumerable<PipeValue> GetDeleteEnumerable(
        PipeContext context, 
        IndexDocument pkIndex, 
        IServicesFactory factory, 
        BsonDocument parameters)
    {
        var query = new Query 
        { 
            Collection = _collectionName, 
            Select = SelectFields.Id, 
            Where = _whereExpr
        };

        var optimizator = factory.CreateQueryOptimization();

        var enumerator = optimizator.ProcessQuery(query, parameters);

        while (true)
        {
            var result = enumerator.MoveNext(context);

            if (result.IsEmpty) break;

            if (optimizator.IndexName == "_id")
            {
                yield return result;
            }
            else
            {
                var id = result.Value["_id"];

                // get PK index node based on _id value
                var node = context.IndexService.Find(pkIndex, id, false, Query.Ascending);

                yield return new PipeValue(node.IndexNodeID, node.DataBlockID, id);
            }
        }
    }

    public ValueTask<IDataReader> ExecuteReaderAsync(IServicesFactory factory, BsonDocument parameters) => throw new NotSupportedException();
}
