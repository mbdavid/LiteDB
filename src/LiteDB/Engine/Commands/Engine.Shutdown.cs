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
        var stream = diskService.GetDiskWriter();

        // must enter in exclusive lock
        await lockService.EnterExclusiveAsync();

        // set engine state to shutdown
        _factory.State = EngineState.Shutdown;

        // do checkpoint
        await logService.CheckpointAsync(true, false);

        // persist all dirty amp into disk
        allocationMapService.WriteAllChanges();

        // if file was changed, update file header check byte
        if (_factory.FileHeader.IsDirty)
        {
            stream.WriteFlag(FileHeader.P_IS_DIRTY, 0);
        
            _factory.FileHeader.IsDirty = false;
        }

        // release exclusive
        lockService.ExitExclusive();

        // call sync dispose (release disk/memory)
        _factory.Dispose();
    }
}