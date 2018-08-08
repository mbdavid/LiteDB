using System;

namespace LiteDB.Engine
{
    /// <summary>
    /// Interface for create custom engine settgins in future
    /// </summary>
    public interface IEngineSettings
    {
        /// <summary>
        /// When wal file get this checkpoint limit, write over data disk
        /// </summary>
        int Checkpoint { get; set; }

        /// <summary>
        /// Indicate that engine will do a checkpoint on dispose database
        /// </summary>
        bool CheckpointOnShutdown { get; set; }

        /// <summary>
        /// If database is new, initialize with allocated space
        /// </summary>
        long InitialSize { get; set; }

        /// <summary>
        /// "limit size": Max limit of datafile
        /// </summary>
        long LimitSize { get; set; }

        /// <summary>
        /// Get/Set custom instance for Logger
        /// </summary>
        Logger Log { get; set; }

        /// <summary>
        /// Debug messages from database
        /// </summary>
        byte LogLevel { get; set; }

        /// <summary>
        /// Define max pages a trasaction must keep in-memory before flush to WAL file. Must be larger than 100
        /// </summary>
        int MaxMemoryTransactionSize { get; set; }

        /// <summary>
        /// Timeout for waiting unlock operations
        /// </summary>
        TimeSpan Timeout { get; set; }

        /// <summary>
        /// Returns date in UTC timezone from BSON deserialization
        /// </summary>
        bool UtcDate { get; set; }

        /// <summary>
        /// Get custom implementation of DiskFactory for this engine settings
        /// </summary>
        IDiskFactory GetDiskFactory();
    }
}