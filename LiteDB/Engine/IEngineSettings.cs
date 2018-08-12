using System;

namespace LiteDB.Engine
{
    public interface IEngineSettings
    {
        int Checkpoint { get; set; }
        bool CheckpointOnShutdown { get; set; }
        long InitialSize { get; set; }
        long LimitSize { get; set; }
        Logger Log { get; set; }
        byte LogLevel { get; set; }
        int MaxMemoryTransactionSize { get; set; }
        TimeSpan Timeout { get; set; }
        bool UtcDate { get; set; }

        IDiskFactory GetDiskFactory();
    }
}