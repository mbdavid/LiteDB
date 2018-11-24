using System;
using System.Text.RegularExpressions;

namespace LiteDB.Engine
{
    internal class CollectionIndex
    {
        /// <summary>
        /// Index name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Get index expression (path or expr)
        /// </summary>
        public string Expression { get; }

        /// <summary>
        /// Indicate if this index has distinct values only
        /// </summary>
        public bool Unique { get; }

        /// <summary>
        /// Head page address for this index
        /// </summary>
        public PageAddress Head { get; }

        /// <summary>
        /// Retain Head index node instance to avoid re-read all times (this node never change)
        /// </summary>
        public IndexNode HeadNode { get; set; }

        /// <summary>
        /// A link pointer to tail node
        /// </summary>
        public PageAddress Tail { get; }

        /// <summary>
        /// Retain Tail index node instance to avoid re-read all times (this node never change)
        /// </summary>
        public IndexNode TailNode { get; set; }

        /// <summary>
        /// Get/Set collection max level
        /// </summary>
        public byte MaxLevel { get; set; } = 1;

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

        public CollectionIndex(string name, string expr, bool unique, PageAddress head, PageAddress tail)
        {
            this.Name = name;
            this.Expression = expr;
            this.Unique = unique;
            this.Head = head;
            this.Tail = tail;
        }
    }
}