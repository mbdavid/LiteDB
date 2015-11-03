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
        /// This size is used bytes in header pages 17 bytes
        /// </summary>
        public const int PAGE_HEADER_SIZE = 17;

        /// <summary>
        /// Bytes avaiable to store data removing page header size - 4079 bytes
        /// </summary>
        public const int PAGE_AVAILABLE_BYTES = PAGE_SIZE - PAGE_HEADER_SIZE;

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
        /// Its Int32 but writes in UInt16
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// Used to find a free page using only header search [used in FreeList]
        /// Its Int32 but writes in UInt16
        /// Its updated when a page modify content length (add/remove items)
        /// </summary>
        public int FreeBytes { get; set; }

        /// <summary>
        /// Indicate that this page is dirty (was modified) and must persist when commited [not-persistable]
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Used in cache system to manager circular cache (CacheService only will use) [not-persistable]
        /// </summary>
        public uint MRU { get; set; }

        /// <summary>
        /// This is the data when read first from disk - used to journal operations (IDiskService only will use)
        /// </summary>
        //TODO public byte[] OriginalBuffer { get; private set; }

        public BasePage()
        {
            this.PrevPageID = uint.MaxValue;
            this.NextPageID = uint.MaxValue;
            this.PageType = LiteDB.PageType.Empty;
            this.ItemCount = 0;
            this.FreeBytes = PAGE_AVAILABLE_BYTES;
        }

        /// <summary>
        /// Every page must imeplement this ItemCount + FreeBytes
        /// Must be called after Items are updates (insert/deletes) to keep variables ItemCount and FreeBytes synced
        /// </summary>
        public virtual void UpdateItemCount()
        {
            this.ItemCount = 0;
            this.FreeBytes = PAGE_AVAILABLE_BYTES;
        }

        /// <summary>
        /// Clear page content (using when delete a page)
        /// </summary>
        public virtual void Clear()
        {
            this.PrevPageID = uint.MaxValue;
            this.NextPageID = uint.MaxValue;
            this.PageType = PageType.Empty;
            this.FreeBytes = PAGE_AVAILABLE_BYTES;
            this.ItemCount = 0;
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
            page.FreeBytes = this.FreeBytes;
            page.IsDirty = this.IsDirty;

            return page;
        }

        #region Read/Write page

        public virtual void ReadHeader(ByteReader reader)
        {
            this.PageID = reader.ReadUInt32();
            this.PrevPageID = reader.ReadUInt32();
            this.NextPageID = reader.ReadUInt32();
            this.PageType = (PageType)reader.ReadByte();
            this.ItemCount = reader.ReadUInt16();
            this.FreeBytes = reader.ReadUInt16();
        }

        public virtual void WriteHeader(ByteWriter writer)
        {
            writer.Write(this.PageID);
            writer.Write(this.PrevPageID);
            writer.Write(this.NextPageID);
            writer.Write((byte)this.PageType);
            writer.Write((UInt16)this.ItemCount);
            writer.Write((UInt16)this.FreeBytes);
        }

        public virtual void ReadContent(ByteReader reader)
        {
        }

        public virtual void WriteContent(ByteWriter writer)
        {
        }

        public void ReadPage(byte[] buffer)
        {
            var reader = new ByteReader(buffer);

            this.ReadHeader(reader);

            if (this.PageType != LiteDB.PageType.Empty)
            {
                this.ReadContent(reader);
            }
        }

        public byte[] WritePage()
        {
            var writer = new ByteWriter(BasePage.PAGE_SIZE);

            WriteHeader(writer);

            if (this.PageType != LiteDB.PageType.Empty)
            {
                WriteContent(writer);
            }

            return writer.Buffer;
        }

        #endregion
    }
}
