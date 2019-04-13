using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LiteDB.Engine
{
    /// <summary>
    /// Get checkpoint mode operations
    /// </summary>
    internal enum CheckpointMode
    {
        /// <summary>
        /// Full checkpoint will exclusive lock on database, wait all async queue be write to disk, copy all pages from log to data file
        /// </summary>
        Full,

        /// <summary>
        /// Same as full but will force LOG file delete after execute
        /// </summary>
        Shutdown,

        /// <summary>
        /// Incremental checkpoint will work only over confirmed transactions (already in-log-disk pages); 
        /// Can clear log file if there is no write during checkpoint operation
        /// Do not do any lock. Default mode
        /// </summary>
        Incremental
    }
}