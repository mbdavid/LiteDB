using System;
using System.Collections.Generic;

namespace LiteDB
{
    /// <summary>
    /// Represent a index information
    /// </summary>
    public class IndexInfo
    {
        internal IndexInfo(CollectionIndex index)
        {
            this.Slot = index.Slot;
            this.Field = index.Field;
            this.Options = index.Options.Clone();
            this.Stats = null;
        }

        /// <summary>
        /// Slot number of index
        /// </summary>
        public int Slot { get; private set; }

        /// <summary>
        /// Field index name
        /// </summary>
        public string Field { get; private set; }

        /// <summary>
        /// Index options
        /// </summary>
        public IndexOptions Options { get; private set; }

        /// <summary>
        /// Index stats
        /// </summary>
        public IndexStats Stats { get; internal set; }

        public class IndexStats
        {
            /// <summary>
            /// Number of pages used in this index
            /// </summary>
            public int Pages { get; set; }

            /// <summary>
            /// Bytes allocated to this index
            /// </summary>
            public long Allocated { get; set; }

            /// <summary>
            /// Key average size (in bytes)
            /// </summary>
            public int KeyAverageSize { get; set; }
        }
    }
}