namespace LiteDB.Engine;

internal class PragmaStatement : IEngineStatement
{
    private readonly string _name;
    private readonly int _value;

    public EngineStatementType StatementType => EngineStatementType.Pragma;

    /// <summary>
    /// Update pragma value
    /// </summary>
    public PragmaStatement(string name, BsonValue value)
    {
        _name = name;
        _value = value;
    }

    public async ValueTask<int> ExecuteAsync(IServicesFactory factory, BsonDocument parameters)
    {
        using var _pc = PERF_COUNTER(190, nameof(ExecuteAsync), nameof(PragmaStatement));

        var diskService = factory.DiskService;
        var pragmas = factory.Pragmas;

        // update pragma value
        pragmas.Set(_name, _value);

        // write pragmas on disk
        await diskService.WritePragmasAsync(pragmas);

        return 1;
    }

    public ValueTask<IDataReader> ExecuteReaderAsync(IServicesFactory factory, BsonDocument parameters) => throw new NotSupportedException();
}
