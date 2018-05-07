using System;
using System.Text.RegularExpressions;

namespace LiteDB
{
    internal class CollectionIndex
    {
        public static Regex IndexPattern = new Regex(@"^[\w](\.?[\w\$][\w-]*){0,59}$", RegexOptions.Compiled);

        /// <summary>
        /// Total indexes per collection - it's fixed because I will used fixed arrays allocations
        /// </summary>
        public const int INDEX_PER_COLLECTION = 16;

        /// <summary>
        /// Represent slot position on index array on dataBlock/collection indexes - non-persistable
        /// </summary>
        public int Slot { get; set; }

        /// <summary>
        /// Field name
        /// </summary>
        public string Field { get; set; }

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
        /// Returns if this index slot is empty and can be used as new index
        /// </summary>
        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(this.Field); }
        }

        /// <summary>
        /// Persist max level used
        /// </summary>
        public byte MaxLevel { get; set; }

        /// <summary>
        /// Get a reference for page
        /// </summary>
        public CollectionPage Page { get; set; }

        public CollectionIndex()
        {
            this.Clear();
        }

        /// <summary>
        /// Clear all index information
        /// </summary>
        public void Clear()
        {
            this.Field = string.Empty;
            this.Expression = string.Empty;
            this.Unique = false;
            this.HeadNode = PageAddress.Empty;
            this.FreeIndexPageID = uint.MaxValue;
            this.MaxLevel = 1;
        }
    }
}