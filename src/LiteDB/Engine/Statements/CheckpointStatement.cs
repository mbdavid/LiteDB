namespace LiteDB.Engine;

internal class CheckpointStatement : IEngineStatement
{
    public EngineStatementType StatementType => EngineStatementType.Checkpoint;

    public CheckpointStatement()
    {
    }

    public async ValueTask<int> ExecuteAsync(IServicesFactory factory, BsonDocument parameters)
    {
        using var _pc = PERF_COUNTER(37, nameof(ExecuteAsync), nameof(CheckpointStatement));

        var lockService = factory.LockService;
        var logService = factory.LogService;

        // checkpoint require exclusive lock (no readers/writers)
        await lockService.EnterExclusiveAsync();

        // do checkpoint and returns how many pages was overrided
        var result = await logService.CheckpointAsync(false, /* true*/ false);

        // release exclusive
        lockService.ExitExclusive();

        return result;
    }

    public ValueTask<IDataReader> ExecuteReaderAsync(IServicesFactory factory, BsonDocument parameters) => throw new NotSupportedException();
}
