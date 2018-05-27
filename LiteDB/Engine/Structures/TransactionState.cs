using System;
using System.Collections.Generic;
using System.IO;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represents a transaction state
    /// </summary>
    public enum TransactionState
    {
        /// <summary>
        /// Transaction was not used yet, have no snap and no locks
        /// </summary>
        New,

        /// <summary>
        /// Transaction are in use with one or more snaps in memory with page in use
        /// </summary>
        InUse,

        /// <summary>
        /// Transaction was fully commited into wal disk and confirmed and all snaps are unlocked
        /// </summary>
        Commited,

        /// <summary>
        /// Transaction was completed aborted and all pages are clear. If any page are persisted in wal file has no confirmation and will not valid in next checkpoint
        /// Used NewPages are reverted in header page
        /// All snaps are clear and unlocked
        /// </summary>
        Aborted,

        /// <summary>
        /// Transaction was completed disposed and current thread now can open new transaction
        /// </summary>
        Disposed
    }
}