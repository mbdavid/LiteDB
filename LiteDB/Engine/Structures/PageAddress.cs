using System;
using System.Collections.Generic;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represents a page address inside a page structure - index could be byte offset position OR index in a list (6 bytes)
    /// </summary>
    internal struct PageAddress
    {
        public const int SIZE = 6;

        public static PageAddress Empty => new PageAddress(uint.MaxValue, ushort.MaxValue);

        /// <summary>
        /// PageID (4 bytes)
        /// </summary>
        public uint PageID;

        /// <summary>
        /// Index inside page (2 bytes)
        /// </summary>
        public ushort Index;

        /// <summary>
        /// Returns true if this PageAdress is empty value
        /// </summary>
        public bool IsEmpty => this.PageID == uint.MaxValue && this.Index == ushort.MaxValue;

        public override bool Equals(object obj)
        {
            var other = (PageAddress)obj;

            return this.PageID == other.PageID && this.Index == other.Index;
        }

        public static bool operator ==(PageAddress lhs, PageAddress rhs)
        {
            return lhs.PageID == rhs.PageID && lhs.Index == rhs.Index;
        }

        public static bool operator !=(PageAddress lhs, PageAddress rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (int)this.PageID;
                hash = hash * 23 + this.Index;
                return hash;
            }
        }

        public PageAddress(uint pageID, ushort index)
        {
            this.PageID = pageID;
            this.Index = index;
        }

        public override string ToString()
        {
            return this.IsEmpty ? "(empty)" : this.PageID.ToString() + ":" + this.Index.ToString();
        }
    }
}