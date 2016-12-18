using System;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Used to control lock state. Based on SQLite
    /// http://www.sqlite.org/lockingv3.html
    /// </summary>
    public enum LockState
    {
        /// <summary>
        /// No lock - initial state
        /// </summary>
        Unlocked,

        /// <summary>
        /// FileAccess.Read | FileShared.ReadWrite
        /// </summary>
        Shared,

        /// <summary>
        /// FileAccess.Write | FileShared.Read
        /// </summary>
        Reserved,

        /// <summary>
        /// FileAccess.Write | FileShared.None
        /// </summary>
        Exclusive
    }
}