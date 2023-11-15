namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public async Task ShutdownAsync()
    {
        if (_factory.State != EngineState.Open) throw ERR("must be open");

        var lockService = _factory.LockService;
        var diskService = _factory.DiskService;
        var logService = _factory.LogService;
        var allocationMapService = _factory.AllocationMapService;

        // must enter in exclusive lock
        await lockService.EnterExclusiveAsync();

        // set engine state to shutdown
        _factory.State = EngineState.Shutdown;

        // do checkpoint
        await logService.CheckpointAsync(true, false);

        // persist all dirty amp into disk
        allocationMapService.WriteAllChangesAsync();

        // if file was changed, update pragma isdirty check byte
        if (_factory.Pragmas.IsDirty)
        {
            _factory.Pragmas.IsDirty = false;

            diskService.WritePragmasAsync(_factory.Pragmas);
        }

        // release exclusive
        lockService.ExitExclusive();

        // call sync dispose (release disk/memory)
        _factory.Dispose();
    }
}