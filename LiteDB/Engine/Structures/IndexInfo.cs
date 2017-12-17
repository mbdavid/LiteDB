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
            this.Expression = index.Expression;
            this.Unique = index.Unique;
            this.MaxLevel = index.MaxLevel;
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
        /// Index Expression
        /// </summary>
        public string Expression { get; private set; }

        /// <summary>
        /// Index is Unique?
        /// </summary>
        public bool Unique { get; private set; }

        /// <summary>
        /// Indicate max level used in skip-list
        /// </summary>
        public byte MaxLevel { get; private set; }
    }
}