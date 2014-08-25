using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class CollectionIndex
    {
        /// <summary>
        /// Total indexes per collection - it's fixed because I will used fixed arrays allocations
        /// </summary>
        public const int INDEX_PER_COLLECTION = 8;

        /// <summary>
        /// Field name
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Unique keys?
        /// </summary>
        public bool Unique { get; set; }

        /// <summary>
        /// Head page address for this index
        /// </summary>
        public PageAddress HeadNode { get; set; }

        /// <summary>
        /// Get a reference for the free list index page - its private list per collection/index (must be a Field to be used as reference parameter)
        /// </summary>
        public uint FreeIndexPageID;

        /// <summary>
        /// Returns if this index slot is empty and can be used as new index
        /// </summary>
        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(this.Field); }
        }

        /// <summary>
        /// Get a reference for page
        /// </summary>
        public CollectionPage Page { get; set; }

        public CollectionIndex()
        {
            this.Field = string.Empty;
            this.Unique = false;
            this.HeadNode = PageAddress.Empty;
            this.FreeIndexPageID = uint.MaxValue;
        }
    }
}
