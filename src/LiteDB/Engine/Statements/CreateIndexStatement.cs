namespace LiteDB.Engine;

internal class CreateIndexStatement : IEngineStatement
{
    private readonly string _collectionName;
    private readonly string _indexName;
    private readonly BsonExpression _expression;
    private readonly bool _unique;

    public EngineStatementType StatementType => EngineStatementType.CreateIndex;

    public CreateIndexStatement(string collectionName, string indexName, BsonExpression expression, bool unique)
    {
        if (!collectionName.IsIdentifier()) throw ERR("Invalid collection name");
        if (!indexName.IsIdentifier(INDEX_MAX_NAME_LENGTH)) throw ERR("Invalid index name");
        if (!expression.GetInfo().IsIndexable) throw ERR("expression must be indexable");

        _collectionName = collectionName;
        _indexName = indexName;
        _expression = expression;
        _unique = unique;
    }

    public async ValueTask<int> ExecuteAsync(IServicesFactory factory, BsonDocument parameters)
    {
        using var _pc = PERF_COUNTER(33, nameof(ExecuteAsync), nameof(CreateIndexStatement));

        // dependency injection
        var autoIdService = factory.AutoIdService;
        var masterService = factory.MasterService;
        var monitorService = factory.MonitorService;
        var collation = factory.FileHeader.Collation;

        // get current $master
        var master = masterService.GetMaster(false);

        // if collection do not exists, retruns 0
        if (!master.Collections.TryGetValue(_collectionName, out var collection)) throw ERR($"colecao {_collectionName} nao encontrada");

        // create a new transaction locking colID
        var transaction = await monitorService.CreateTransactionAsync(new byte[] { MASTER_COL_ID, collection.ColID });

        var dataService = factory.CreateDataService(transaction);
        var indexService = factory.CreateIndexService(transaction);

        // create new index (head/tail)
        var (head, tail) = indexService.CreateHeadTailNodes(collection.ColID);

        // get a free index slot
        var freeIndexSlot = (byte)Enumerable.Range(1, INDEX_MAX_LEVELS)
            .Where(x => collection.Indexes.Any(y => y.Slot == x) == false)
            .FirstOrDefault();

        // create new collection in $master and returns a new master document
        var indexDocument = new IndexDocument()
        {
            Slot = freeIndexSlot,
            Name = _indexName,
            Expression = _expression,
            Unique = _unique,
            HeadIndexNodeID = head,
            TailIndexNodeID = tail
        };

        // add new index into master model
        collection.Indexes.Add(indexDocument);

        // write master collection into pages inside transaction
        masterService.WriteCollection(master, transaction);

        // create pipe context
        var pipeContext = new PipeContext(dataService, indexService, BsonDocument.Empty);

        var exprInfo = _expression.GetInfo();
        var fields = exprInfo.RootFields;

        // get index nodes created
        var counter = 0;

        // read all documents based on a full PK scan
        using (var enumerator = new IndexNodeEnumerator(indexService, collection.PK))
        {
            while (enumerator.MoveNext())
            {
                var pkIndexNode = enumerator.Current;
                var dataBlockID = pkIndexNode.DataBlockID;
                var defrag = false;

                // read document fields
                var docResult = dataService.ReadDocument(pkIndexNode.DataBlockID, fields);

                if (docResult.Fail) throw docResult.Exception;

                // get all keys for this index
                var keys = _expression.GetIndexKeys(docResult.Value.AsDocument, collation);

                var first = IndexNodeResult.Empty;
                var last = IndexNodeResult.Empty;

                foreach (var key in keys)
                {
                    var node = indexService.AddNode(collection.ColID, indexDocument, key, dataBlockID, last, out defrag);

                    // ensure execute reload on indexNode after any defrag
                    if (defrag && pkIndexNode.IndexNodeID.PageID == node.IndexNodeID.PageID)
                    {
                        pkIndexNode.Reload();
                    }

                    // keep first node to add in NextNode list (after pk)
                    if (first.IsEmpty) first = node;

                    last = node;
                    counter++;
                }

                ENSURE(first.IsEmpty == false);
                //pkIndexNode.Reload();

                pkIndexNode.NextNodeID = first.IndexNodeID;

                unsafe
                {
                    pkIndexNode.Page->IsDirty = true;
                }

                // do a safepoint after insert each document
                if (monitorService.Safepoint(transaction))
                {
                    await transaction.SafepointAsync();
                }
            }
        }

        // write all dirty pages into disk
        await transaction.CommitAsync();

        // release transaction
        monitorService.ReleaseTransaction(transaction);

        // TODO: retornar em formato de array? quem sabe a entrada pode ser um BsonValue (array/document) e o retorno o mesmo
        return counter;

    }

    public ValueTask<IDataReader> ExecuteReaderAsync(IServicesFactory factory, BsonDocument parameters) => throw new NotSupportedException();
}
