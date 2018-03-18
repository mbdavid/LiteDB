using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace LiteDB
{
    /// <summary>
    /// Represent a simple structure to store added/removed pages in a transaction. One instance per transaction
    /// </summary>
    internal class TransactionPages
    {
        /// <summary>
        /// Get how many pages are involved in this current transaction across all snapshots
        /// </summary>
        public int PageCount = 0;

        /// <summary>
        /// Contains all dirty pages already persist in WAL (used in all snapshots)
        /// </summary>
        public Dictionary<uint, PagePosition> DirtyPagesWal { get; private set; } = new Dictionary<uint, PagePosition>();

        /// <summary>
        /// Handle created pages during transaction (for rollback) - Is a list because order is important
        /// </summary>
        public List<uint> NewPages { get; private set; } = new List<uint>();

        /// <summary>
        /// First deleted page 
        /// </summary>
        public BasePage FirstDeletedPage { get; set; }

        /// <summary>
        /// Last deleted page
        /// </summary>
        public BasePage LastDeletedPage { get; set; }

        /// <summary>
        /// Get deleted page count
        /// </summary>
        public int DeletedPages { get; set; }

        /// <summary>
        /// New collections added in this transaction
        /// </summary>
        public Dictionary<string, CollectionPage> NewCollections { get; set; } = new Dictionary<string, CollectionPage>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Deleted collections in this transaction
        /// </summary>
        public Dictionary<string, CollectionPage> DeletedCollections { get; set; } = new Dictionary<string, CollectionPage>(StringComparer.OrdinalIgnoreCase);
    }
}