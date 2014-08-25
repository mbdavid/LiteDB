using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal enum PageType { Empty = 0, Header = 1, Collection = 2, Index = 3, Data = 4, Extend = 5 }

    internal class BasePage
    {
        #region Page Constants

        /// <summary>
        /// The size of each page in disk - 4096 is NTFS default
        /// </summary>
        public const int PAGE_SIZE = 4096;

        /// <summary>
        /// This size is used bytes in header pages [17 bytes + 19 reserved] 
        /// </summary>
        public const int PAGE_HEADER_SIZE = 36;

        /// <summary>
        /// Bytes avaiable to store data removing page header size
        /// </summary>
        public const int PAGE_AVAILABLE_BYTES = PAGE_SIZE - PAGE_HEADER_SIZE;

        /// <summary>
        /// If a page has less that this number, it's considered full page for new items. Can be used only for update (DataPage) ~ 15% PAGE_SIZE
        /// </summary>
        public const int RESERVED_BYTES = 600;

        #endregion

        /// <summary>
        /// Represent page number - start in 0 with HeaderPage [4 bytes]
        /// </summary>
        public uint PageID { get; set; }

        /// <summary>
        /// Represent the previous page. Used for page-sequences - MaxValue represent that has NO previous page [4 bytes]
        /// </summary>
        public uint PrevPageID { get; set; }

        /// <summary>
        /// Represent the next page. Used for page-sequences - MaxValue represent that has NO next page [4 bytes]
        /// </summary>
        public uint NextPageID { get; set; }

        /// <summary>
        /// Indicate the page type [1 byte]
        /// </summary>
        public PageType PageType { get; set; }

        /// <summary>
        /// Used for all pages to count itens inside this page(bytes, nodes, blocks, ...)
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// Must be overite for each page. Used to find a free page using only header search [used in FreeList]
        /// </summary>
        public virtual int FreeBytes { get; set; }

        /// <summary>
        /// Indicate that this page is dirty (was modified) and must persist when commited [not-persistable]
        /// </summary>
        public bool IsDirty { get; set; }

        public BasePage()
        {
            this.PrevPageID = uint.MaxValue;
            this.NextPageID = uint.MaxValue;
            this.PageType = LiteDB.PageType.Empty;
            this.FreeBytes = PAGE_AVAILABLE_BYTES;
        }

        /// <summary>
        /// Used in all specific page to update ItemCount before write on disk
        /// </summary>
        protected virtual void UpdateItemCount()
        {
            // must be implemented in all pages types
            this.ItemCount = 0;
        }

        /// <summary>
        /// Clear page content (using when delete a page)
        /// </summary>
        public virtual void Clear()
        {
            this.PrevPageID = uint.MaxValue;
            this.NextPageID = uint.MaxValue;
            this.PageType = LiteDB.PageType.Empty;
            this.FreeBytes = PAGE_AVAILABLE_BYTES;
        }

        /// <summary>
        /// Create a new espefic page, copy all header content
        /// </summary>
        public T CopyTo<T>()
            where T : BasePage, new()
        {
            var page = new T();
            page.PageID = this.PageID;
            page.PrevPageID = this.PrevPageID;
            page.NextPageID = this.NextPageID;
            page.PageType = this.PageType;
            page.ItemCount = this.ItemCount;
            page.IsDirty = this.IsDirty;

            return page;
        }

        #region Page Header

        public virtual void ReadHeader(BinaryReader reader)
        {
            this.PageID = reader.ReadUInt32();
            this.PrevPageID = reader.ReadUInt32();
            this.NextPageID = reader.ReadUInt32();
            this.PageType = (PageType)reader.ReadByte();
            this.ItemCount = reader.ReadUInt16();
            this.FreeBytes = reader.ReadInt32();
        }

        public virtual void WriteHeader(BinaryWriter writer)
        {
            writer.Write(this.PageID);
            writer.Write(this.PrevPageID);
            writer.Write(this.NextPageID);
            writer.Write((byte)this.PageType);
            UpdateItemCount(); // updating ItemCount before save on disk
            writer.Write((UInt16)this.ItemCount);
            writer.Write(this.FreeBytes);
        }

        #endregion

        #region Page Content

        public virtual void ReadContent(BinaryReader reader)
        {
        }

        public virtual void WriteContent(BinaryWriter writer)
        {
        }

        #endregion
    }
}
