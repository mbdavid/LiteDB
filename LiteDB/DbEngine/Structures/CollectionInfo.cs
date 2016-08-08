using System;
using System.Collections.Generic;

namespace LiteDB
{
    /// <summary>
    /// Represent a index information
    /// </summary>
    public class CollectionInfo
    {
        /// <summary>
        /// Collection name
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Number of document in collection
        /// </summary>
        public long DocumentsCount { get; internal set; }

        /// <summary>
        /// Avarage document size (in bytes)
        /// </summary>
        public int DocumentAverageSize { get; internal set; }

        /// <summary>
        /// Indexes informations
        /// </summary>
        public List<IndexInfo> Indexes { get; internal set; }

        /// <summary>
        /// Total pages used by this collection
        /// </summary>
        public int TotalPages { get; internal set; }

        /// <summary>
        /// Total bytes allocated to this collection
        /// </summary>
        public long TotalAllocated { get; internal set; }

        /// <summary>
        /// Total bytes free to this collection
        /// </summary>
        public long TotalFree { get; internal set; }

        /// <summary>
        /// Number of pages of each type
        /// </summary>
        public Dictionary<string, int> Pages { get; internal set; }

        /// <summary>
        /// Allocated space (in bytes) of each page in collection
        /// </summary>
        public Dictionary<string, long> Allocated { get; internal set; }

        /// <summary>
        /// Avaiable free space (in bytes) of each page in collection
        /// </summary>
        public Dictionary<string, long> Free { get; internal set; }
    }
}