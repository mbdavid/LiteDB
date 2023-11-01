using System.Net.Sockets;

namespace LiteDB.Engine;

internal class PipelineBuilder
{
    // dependency injections
    private IMasterService _masterService;
    private ISortService _sortService;
    private Collation _collation;

    private IDocumentStore _store;

    private BsonDocument _queryParameters;
    private IPipeEnumerator? _enumerator;

    public PipelineBuilder(
        IMasterService masterService,
        ISortService sortService,
        Collation collation,
        IDocumentStore store, 
        BsonDocument queryParameters)
    {
        _masterService = masterService;
        _sortService = sortService;
        _collation = collation;
        _store = store;
        _queryParameters = queryParameters;
    }

    public IPipeEnumerator GetPipeEnumerator() => _enumerator ?? throw ERR("No pipe to be executed");

    /// <summary>
    /// Create document lookup using DataService to get values
    /// </summary>
    public IDocumentLookup CreateDocumentLookup(string[] fields)
        => new DataLookup(fields);

    /// <summary>
    /// Create document lookup creating a fake document based only in index key only
    /// </summary>
    public IDocumentLookup CreateIndexLookup(string field)
        => new IndexLookup(field);

    /// <summary>
    /// Add index pipe based on predicate expression (BinaryBsonExpression) or index scan expression
    /// Delect indexDocument based on left/right side of predicate (should exist in collection)
    /// </summary>
    public PipelineBuilder AddIndex(BsonExpression expr, int order, bool returnKey)
    {
        if (expr is BinaryBsonExpression predicate)
        {
            // predicate expression eg: "$._id = 123"
            this.AddIndexPredicate(predicate, order, returnKey);
        }
        else
        {
            // full index scan eg: "$._id"
            var indexDocument = _store.GetIndexes().FirstOrDefault(x => x.Expression == expr) ??
                throw ERR($"No index found for this expression: {expr}");

            _enumerator = new IndexAllEnumerator(indexDocument, order, returnKey);
        }

        return this;
    }

    /// <summary>
    /// Add a predicate index based on left/right expression sides to look for this index expression on collection
    /// </summary>
    private void AddIndexPredicate(BinaryBsonExpression predicate, int order, bool returnKey)
    {
        // try get index from left
        var indexes = _store.GetIndexes();
        var indexDocument = indexes.FirstOrDefault(x => x.Expression == predicate.Left);

        if (indexDocument is not null)
        {
            var value = predicate.Right.Execute(null, _queryParameters, _collation);

            _enumerator = this.CreateIndex(indexDocument, value, predicate.Type, order, returnKey);
        }
        else
        {
            // try get index from right
            indexDocument = indexes.FirstOrDefault(x => x.Expression == predicate.Right) ??
                throw ERR($"No index found for this expression: {predicate}");

            // invert expression
            var exprType = predicate.Type switch
            {
                BsonExpressionType.GreaterThan => BsonExpressionType.LessThan,
                BsonExpressionType.GreaterThanOrEqual => BsonExpressionType.LessThanOrEqual,
                BsonExpressionType.LessThan => BsonExpressionType.GreaterThan,
                BsonExpressionType.LessThanOrEqual => BsonExpressionType.GreaterThanOrEqual,
                _ => predicate.Type
            };

            var value = predicate.Left.Execute(null, _queryParameters, _collation);

            _enumerator = this.CreateIndex(indexDocument, value, predicate.Type, order, returnKey);
        }
    }

    public IPipeEnumerator CreateIndex(IndexDocument indexDocument, BsonValue value, BsonExpressionType exprType, int order, bool returnKey)
    {
        return (exprType, value.Type) switch
        {
            (BsonExpressionType.Equal, _) => new IndexEqualsEnumerator(value, indexDocument, _collation, returnKey),
            (BsonExpressionType.Between, _) => new IndexRangeEnumerator(value.AsArray[0], value.AsArray[1], true, true, order, indexDocument, _collation, returnKey),
            (BsonExpressionType.Like, _) => new IndexLikeEnumerator(value, indexDocument, _collation, order, returnKey),
            (BsonExpressionType.GreaterThan, _) => new IndexRangeEnumerator(value, BsonValue.MaxValue, false, false, order, indexDocument, _collation, returnKey),
            (BsonExpressionType.GreaterThanOrEqual, _) => new IndexRangeEnumerator(value, BsonValue.MaxValue, true, false, order, indexDocument, _collation, returnKey),
            (BsonExpressionType.LessThan, _) => new IndexRangeEnumerator(BsonValue.MinValue, value, false, false, order, indexDocument, _collation, returnKey),
            (BsonExpressionType.LessThanOrEqual, _) => new IndexRangeEnumerator(BsonValue.MinValue, value, false, true, order, indexDocument, _collation, returnKey),
            (BsonExpressionType.NotEqual, _) => new IndexScanEnumerator(indexDocument, x => x.CompareTo(value, _collation) != 0, order, returnKey),
            (BsonExpressionType.In, BsonType.Array) => new IndexInEnumerator(value.AsArray, indexDocument, _collation, returnKey),
            (BsonExpressionType.In, _) => new IndexEqualsEnumerator(value, indexDocument, _collation, returnKey),
            _ => throw ERR($"There is no index for {exprType} predicate")
        };
    }

    /// <summary>
    /// Add a new document lookup deserializing full document
    /// </summary>
    public PipelineBuilder AddLookup()
        => this.AddLookup(Array.Empty<string>());

    /// <summary>
    /// Add a new document lookup deserializing selected fields only
    /// </summary>
    public PipelineBuilder AddLookup(string[] fields)
        => this.AddLookup(this.CreateDocumentLookup(fields));

    /// <summary>
    /// Add a new index lookup using field name
    /// </summary>
    public PipelineBuilder AddLookup(string field)
        => this.AddLookup(this.CreateIndexLookup(field));

    /// <summary>
    /// Add an existed lookup implementation
    /// </summary>
    public PipelineBuilder AddLookup(IDocumentLookup lookup)
    {
        if (_enumerator is null) throw ERR("Start pipeline using AddIndex");

        _enumerator = new LookupEnumerator(lookup, _enumerator);

        return this;
    }

    /// <summary>
    /// Add filter pipeline, removing when filter predicate returns diferent from True
    /// </summary>
    public PipelineBuilder AddFilter(BsonExpression filter)
    {
        if (_enumerator is null) throw ERR("Start pipeline using AddIndex");

        _enumerator = new FilterEnumerator(filter, _enumerator, _collation);

        return this;
    }

    /// <summary>
    /// Skip a number of documents before return first element
    /// </summary>
    public PipelineBuilder AddOffset(int offset)
    {
        if (_enumerator is null) throw ERR("Start pipeline using AddIndex");

        _enumerator = new OffsetEnumerator(offset, _enumerator);

        return this;
    }

    /// <summary>
    /// Stop pipeline after reatch this limit documents
    /// </summary>
    public PipelineBuilder AddLimit(int limit)
    {
        if (_enumerator is null) throw ERR("Start pipeline using AddIndex");

        _enumerator = new LimitEnumerator(limit, _enumerator);

        return this;
    }

    /// <summary>
    /// Order documents according expression/order
    /// </summary>
    public PipelineBuilder AddOrderBy(OrderBy orderBy)
    {
        if (_enumerator is null) throw ERR("Start pipeline using AddIndex");

        _enumerator = new OrderByEnumerator(orderBy, _enumerator, _sortService);

        return this;
    }

    /// <summary>
    /// Include DbRef reference when pathExpr results in a DbRef document
    /// </summary>
    public PipelineBuilder AddInclude(BsonExpression pathExpr)
    {
        if (_enumerator is null) throw ERR("Start pipeline using AddIndex");

        _enumerator = new IncludeEnumerator(pathExpr, _enumerator, _masterService, _collation);

        return this;
    }

    /// <summary>
    /// Add multiple aggregate functions into pipeline. Aggregate functions are caculated according orderer input 
    /// </summary>
    public PipelineBuilder AddAggregate(BsonExpression keyExpr, SelectFields fields)
    {
        if (_enumerator is null) throw ERR("Start pipeline using AddIndex");

        _enumerator = new AggregateEnumerator(keyExpr, fields, _enumerator, _collation);

        return this;
    }

    /// <summary>
    /// Add document transform create a new document ouput
    /// </summary>
    public PipelineBuilder AddTransform(SelectFields fields)
    {
        if (_enumerator is null) throw ERR("Start pipeline using AddIndex");

        _enumerator = new TransformEnumerator(fields, _collation, _enumerator);

        return this;
    }
}
