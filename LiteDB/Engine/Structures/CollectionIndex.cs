using System;
using System.Text.RegularExpressions;

namespace LiteDB.Engine
{
    internal class CollectionIndex
    {
        /// <summary>
        /// Index name
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Get index expression (path or expr)
        /// </summary>
        public string Expression { get; set; } = "";

        /// <summary>
        /// Indicate if this index has distinct values only
        /// </summary>
        public bool Unique { get; set; }

        /// <summary>
        /// Head page address for this index
        /// </summary>
        public PageAddress HeadNode { get; set; } = PageAddress.Empty;

        /// <summary>
        /// A link pointer to tail node
        /// </summary>
        public PageAddress TailNode { get; set; } = PageAddress.Empty;

        /// <summary>
        /// Counter of keys in this index
        /// </summary>
        public uint KeyCount { get; set; } = 0;

        /// <summary>
        /// Counter of unique keys in this index (online but be dirty on delete index nodes... will fix on next analyze)
        /// </summary>
        public uint UniqueKeyCount { get; set; } = 0;

        /// <summary>
        /// Get index density based on KeyCount vs UniqueKeyCount. Value are from 0 to 1.
        /// 0 means completed unique keys (best)
        /// 1 means has only 1 single unique key in all index (worst)
        /// </summary>
        public double Density
        {
            get
            {
                if (this.Unique) return 0;
                if (this.UniqueKeyCount == 0 || this.KeyCount == 0) return 1;

                var density = (double)Math.Min(this.UniqueKeyCount, this.KeyCount) /
                    (double)this.KeyCount;


                return Math.Round(density, 2);
            }
        }

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
    }
}