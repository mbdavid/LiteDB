using System;
using System.Collections.Generic;
using System.IO;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represents a snapshot open mode
    /// </summary>
    public enum SnapshotMode
    {
        /// <summary>
        /// Read only snap with read lock
        /// </summary>
        Read,

        /// <summary>
        /// Read/Write snapshot with reserved lock
        /// </summary>
        Write
    }
}