using System;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Used to control lock state.
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
        Read,

        /// <summary>
        /// FileAccess.Write | FileShared.None
        /// </summary>
        Write
    }
}