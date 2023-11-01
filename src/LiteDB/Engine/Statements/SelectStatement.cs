namespace LiteDB.Engine;

internal class SelectStatement : IEngineStatement
{
    private readonly Query _query;
    private readonly bool _explain;
    private readonly int _fetchSize;

    public EngineStatementType StatementType => EngineStatementType.Select;

    #region Ctor

    public SelectStatement(Query query) : 
        this(query, 1000, false) 
    { 
    }

    public SelectStatement(Query query, bool explain) : 
        this(query, 1000, explain)
    {
    }

    public SelectStatement(Query query, int fetchSize) : 
        this (query, fetchSize, false) 
    {
    }

    public SelectStatement(Query query, int fetchSize, bool explain)
    {
        _query = query;
        _fetchSize = fetchSize;
        _explain = explain;
    }

    #endregion

    public ValueTask<IDataReader> ExecuteReaderAsync(IServicesFactory factory, BsonDocument parameters)
    {
        using var _pc = PERF_COUNTER(31, nameof(ExecuteReaderAsync), nameof(SelectStatement));

        // get dependencies
        var walIndexService = factory.WalIndexService;
        var queryService = factory.QueryService;

        // get next read version without open a new transaction
        var readVersion = walIndexService.GetNextReadVersion();

        // create cursor after query optimizer and create enumerator pipeline
        var cursor = queryService.CreateCursor(_query, parameters, readVersion);

        // create concrete class to reader cursor
        IDataReader reader = _explain ? 
                new BsonScalarReader(cursor.Query.Collection, cursor.GetExplainPlan()) : // for explain plain
                factory.CreateDataReader(cursor, _fetchSize, factory);

        return ValueTask.FromResult(reader);
    }

    public ValueTask<int> ExecuteAsync(IServicesFactory factory, BsonDocument parameters) => throw new NotSupportedException();
}
