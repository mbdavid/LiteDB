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
    }
}