using System.ComponentModel.Design;

namespace LiteDB.Engine;

internal class InsertStatement : IEngineStatement
{
    private readonly IDocumentStore _store;

    private readonly BsonDocument _document;
    private readonly BsonArray _documents;
    private readonly BsonExpression _documentExpr;

    private readonly BsonAutoId _autoId;

    public EngineStatementType StatementType => EngineStatementType.Insert;

    #region Ctor

    public InsertStatement(IDocumentStore store, BsonDocument document, BsonAutoId autoId)
    {
        _store = store;
        _document = document;
        _documents = BsonArray.Empty;
        _documentExpr = BsonExpression.Empty;
        _autoId = autoId;
    }

    public InsertStatement(IDocumentStore store, BsonArray documents, BsonAutoId autoId)
    {
        _store = store;
        _document = BsonDocument.Empty;
        _documents = documents;
        _documentExpr = BsonExpression.Empty;
        _autoId = autoId;
    }

    public InsertStatement(IDocumentStore store, BsonExpression documentExpr, BsonAutoId autoId)
    {
        _store = store;
        _document = BsonDocument.Empty;
        _documents = BsonArray.Empty;
        _documentExpr = documentExpr;
        _autoId = autoId;
    }

    #endregion

    public async ValueTask<int> ExecuteAsync(IServicesFactory factory, BsonDocument parameters)
    {
        using var _pc = PERF_COUNTER(31, nameof(ExecuteAsync), nameof(InsertStatement));

        // dependency injection
        var autoIdService = factory.AutoIdService;
        var masterService = factory.MasterService;
        var monitorService = factory.MonitorService;
        var collation = factory.FileHeader.Collation;

        // initialize document store before
        _store.Initialize(masterService);

        // create a new transaction locking colID
        var transaction = await monitorService.CreateTransactionAsync(new byte[] { _store.ColID });

        // get data/index services from store
        var (dataService, indexService) = _store.GetServices(factory, transaction);

        // get all indexes this store contains
        var indexes = _store.GetIndexes();

        try
        {
            // initialize autoId if needed
            if (autoIdService.NeedInitialize(_store.ColID, _autoId))
            {
                if (indexes.Count > 0)
                {
                    autoIdService.Initialize(_store.ColID, indexes[0].TailIndexNodeID, indexService);
                }
                else
                {
                    autoIdService.Initialize(_store.ColID);
                }
            }

            var docs = this.GetDocuments(parameters, collation);

            foreach (var doc in docs)
            {
                // insert document and all indexes for this document (based on collection indexes)
                InsertInternal(
                    _store.ColID,
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

            monitorService.ReleaseTransaction(transaction);

        }
        catch (Exception ex)
        {
            transaction.Abort();

            monitorService.ReleaseTransaction(transaction);

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
