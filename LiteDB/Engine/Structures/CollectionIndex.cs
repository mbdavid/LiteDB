using System;
using System.Text.RegularExpressions;

namespace LiteDB
{
    internal class CollectionIndex
    {
        public static Regex IndexNamePattern = new Regex(@"^\w{1,32}$", RegexOptions.Compiled);

        /// <summary>
        /// Total indexes per collection - it's fixed because I will used fixed arrays allocations
        /// </summary>
        public const int INDEX_PER_COLLECTION = 32;

        /// <summary>
        /// Represent slot position on index array on dataBlock/collection indexes - non-persistable
        /// </summary>
        public int Slot { get; set; }

        /// <summary>
        /// Index name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get index expression (path or expr)
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// Indicate if this index has distinct values only
        /// </summary>
        public bool Unique { get; set; }

        /// <summary>
        /// Head page address for this index
        /// </summary>
        public PageAddress HeadNode { get; set; }

        /// <summary>
        /// A link pointer to tail node
        /// </summary>
        public PageAddress TailNode { get; set; }

        /// <summary>
        /// Get a reference for the free list index page - its private list per collection/index (must be a Field to be used as reference parameter)
        /// </summary>
        public uint FreeIndexPageID;

        /// <summary>
        /// Persist max level used
        /// </summary>
        public byte MaxLevel { get; set; }

        /// <summary>
        /// Counter of keys in this index
        /// </summary>
        public uint KeyCount { get; set; }

        /// <summary>
        /// Counter of unique keys in this index (update only in analze command)
        /// </summary>
        public uint UniqueKeyCount { get; set; }

        /// <summary>
        /// Returns if this index slot is empty and can be used as new index
        /// </summary>
        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(this.Name); }
        }

        /// <summary>
        /// Get a reference for page
        /// </summary>
        public CollectionPage Page { get; set; }

        public CollectionIndex()
        {
        }

        /// <summary>
        /// Clear all index information
        /// </summary>
        public void Clear()
        {
            this.Name = string.Empty;
            this.Expression = string.Empty;
            this.Unique = false;
            this.HeadNode = PageAddress.Empty;
            this.TailNode = PageAddress.Empty;
            this.FreeIndexPageID = uint.MaxValue;
            this.MaxLevel = 1;
            this.KeyCount = 0;
            this.UniqueKeyCount = 0;
        }

        public CollectionIndex Clone()
        {
            return new CollectionIndex
            {
                Slot = this.Slot,
                Name = this.Name,
                Expression = this.Expression,
                Unique = this.Unique,
                HeadNode = this.HeadNode,
                TailNode = this.TailNode,
                FreeIndexPageID = this.FreeIndexPageID,
                MaxLevel = this.MaxLevel,
                KeyCount = this.KeyCount,
                UniqueKeyCount = this.UniqueKeyCount
            };
        }
    }
}