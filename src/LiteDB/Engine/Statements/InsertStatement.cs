using System.ComponentModel.Design;

namespace LiteDB.Engine;

internal class InsertStatement : IEngineStatement
{
    private readonly string _collectionName;

    private readonly BsonDocument _document;
    private readonly BsonArray _documents;
    private readonly BsonExpression _documentExpr;

    private readonly BsonAutoId _autoId;

    public EngineStatementType StatementType => EngineStatementType.Insert;

    #region Ctor

    public InsertStatement(string collectionName, BsonDocument document, BsonAutoId autoId)
    {
        _collectionName = collectionName;
        _document = document;
        _documents = BsonArray.Empty;
        _documentExpr = BsonExpression.Empty;
        _autoId = autoId;
    }

    public InsertStatement(string collectionName, BsonArray documents, BsonAutoId autoId)
    {
        _collectionName = collectionName;
        _document = BsonDocument.Empty;
        _documents = documents;
        _documentExpr = BsonExpression.Empty;
        _autoId = autoId;
    }

    public InsertStatement(string collectionName, BsonExpression documentExpr, BsonAutoId autoId)
    {
        _collectionName = collectionName;
        _document = BsonDocument.Empty;
        _documents = BsonArray.Empty;
        _documentExpr = documentExpr;
        _autoId = autoId;
    }

    #endregion

    public async ValueTask<int> ExecuteAsync(IServicesFactory factory, BsonDocument parameters)
    {
        using var _pc = PERF_COUNTER(36, nameof(ExecuteAsync), nameof(InsertStatement));

        // dependency injection
        var autoIdService = factory.AutoIdService;
        var masterService = factory.MasterService;
        var monitorService = factory.MonitorService;
        var collation = factory.FileHeader.Collation;

        // get master 
        var master = masterService.GetMaster(false);

        if (!master.Collections.TryGetValue(_collectionName, out var collection))
        {
            // auto create collection using create statement
            var create = new CreateCollectionStatement(_collectionName);

            await create.ExecuteAsync(factory, parameters);

            // update master instance with
            master = masterService.GetMaster(false);

            if (!master.Collections.TryGetValue(_collectionName, out collection))
            {
                throw ERR($"Collection {collection} not found");
            }
        }

        var (colID, indexes) = (collection.ColID, collection.Indexes);

        // create a new transaction locking colID
        using var transaction = await monitorService.CreateTransactionAsync([colID]);

        // get data/index services from store
        var dataService = factory.CreateDataService(transaction);
        var indexService = factory.CreateIndexService(transaction);

        try
        {
            // initialize autoId if needed
            if (autoIdService.NeedInitialize(colID, _autoId))
            {
                if (indexes.Count > 0)
                {
                    autoIdService.Initialize(colID, indexes[0].TailIndexNodeID, indexService);
                }
                else
                {
                    autoIdService.Initialize(colID);
                }
            }

            var docs = this.GetDocuments(parameters, collation);

            foreach (var doc in docs)
            {
                // insert document and all indexes for this document (based on collection indexes)
                InsertInternal(
                    colID,
                    doc,
                    _autoId,
                    indexes,
                    dataService,
                    indexService,
                    autoIdService,
                    collation);

                // do a safepoint after insert each document
                if (monitorService.Safepoint(transaction))
                {
                    await transaction.SafepointAsync();
                }
            }

            // write all dirty pages into disk
            await transaction.CommitAsync();

        }
        catch (Exception ex)
        {
            ex.HandleError(factory);
            throw;
        }

        return 1;
    }

    /// <summary>
    /// Get all documents to be inserted in this statement. Can be a single document, a list of document or an expression value
    /// </summary>
    private IEnumerable<BsonDocument> GetDocuments(BsonDocument parameters, Collation collation)
    {
        // single document
        if (_document.IsEmpty == false)
        {
            yield return _document;
        }
        // list of document
        else if (_documents.Count > 0)
        {
            foreach (var item in _documents)
            {
                if (item is BsonDocument doc)
                {
                    yield return doc;
                }
            }
        }
        // expression document resolver
        else
        {
            var result = _documentExpr.Execute(null, parameters, collation);

            if (result is BsonDocument docResult)
            {
                yield return docResult;
            }
            else if (result is BsonArray arrResult)
            {
                foreach (var item in arrResult)
                {
                    if (item is BsonDocument doc)
                    {
                        yield return doc;
                    }
                }
            }
        }
    }

    /// <summary>
    /// A static function to insert a document and all indexes using only interface services. Will be use in InsertSingle, InsertMany, InsertBulk
    /// </summary>
    public static void InsertInternal(
        byte colID,
        BsonDocument doc, 
        BsonAutoId autoId,
        IReadOnlyList<IndexDocument> indexes,
        IDataService dataService,
        IIndexService indexService,
        IAutoIdService autoIdService, 
        Collation collation)
    {
        using var _pc = PERF_COUNTER(10, nameof(InsertInternal), nameof(InsertStatement));

        // get/set _id
        var id = autoIdService.SetDocumentID(colID, doc, autoId);

        // insert document and get position address
        var dataBlockID = dataService.InsertDocument(colID, doc);

        // insert all indexes (init by PK)
        if (indexes.Count > 0)
        {
            // insert _id as PK and get node to be used 
            var last = indexService.AddNode(colID, indexes[0], id, dataBlockID, IndexNodeResult.Empty, out _);

            for (var i = 1; i < indexes.Count; i++)
            {
                var index = indexes[i];

                // get a single or multiple (distinct) values
                var keys = index.Expression.GetIndexKeys(doc, collation);

                foreach (var key in keys)
                {
                    var node = indexService.AddNode(colID, index, key, dataBlockID, last, out _);

                    last = node;
                }
            }
        }
    }

    public ValueTask<IDataReader> ExecuteReaderAsync(IServicesFactory factory, BsonDocument parameters) => throw new NotSupportedException();
}
