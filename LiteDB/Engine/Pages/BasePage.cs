using System;
using System.Collections.Generic;

namespace LiteDB
{
    public enum PageType { Empty = 0, Header = 1, CollectionList = 6, Collection = 2, Index = 3, Data = 4, Extend = 5 }

    internal abstract class BasePage
    {
        /// <summary>
        /// The size of each page in disk
        /// </summary>
        public const int PAGE_SIZE = 8192;

        /// <summary>
        /// This size is used bytes in header pages 33 bytes (+59 reserved to future use) = 92 bytes
        /// </summary>
        public const int PAGE_HEADER_SIZE = 92;

        /// <summary>
        /// Bytes available to store data removing page header size - 4071 bytes
        /// </summary>
        public const int PAGE_AVAILABLE_BYTES = PAGE_SIZE - PAGE_HEADER_SIZE;

        /// <summary>
        /// Represent page number - start in 0 with HeaderPage [4 bytes]
        /// </summary>
        public uint PageID { get; set; }

        /// <summary>
        /// Indicate the page type [1 byte] - Must be implemented for each page type
        /// </summary>
        public abstract PageType PageType { get; }

        /// <summary>
        /// Represent the previous page. Used for page-sequences - MaxValue represent that has NO previous page [4 bytes]
        /// </summary>
        public uint PrevPageID { get; set; }

        /// <summary>
        /// Represent the next page. Used for page-sequences - MaxValue represent that has NO next page [4 bytes]
        /// </summary>
        public uint NextPageID { get; set; }

        /// <summary>
        /// Used for all pages to count items inside this page(bytes, nodes, blocks, ...) [2 bytes]
        /// Its Int32 but writes in UInt16
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// Used to find a free page using only header search [used in FreeList] [2 bytes]
        /// Its Int32 but writes in UInt16
        /// Its updated when a page modify content length (add/remove items)
        /// </summary>
        public int FreeBytes { get; set; }

        /// <summary>
        /// Represent transaction page ID that was stored [16 bytes]
        /// </summary>
        public Guid TransactionID { get; set; }

        /// <summary>
        /// Set this pages that was changed and must be persist in disk [not peristable]
        /// </summary>
        public bool IsDirty { get; set; }

        public BasePage(uint pageID)
        {
            this.PageID = pageID;
            this.PrevPageID = uint.MaxValue;
            this.NextPageID = uint.MaxValue;
            this.ItemCount = 0;
            this.FreeBytes = PAGE_AVAILABLE_BYTES;
            this.TransactionID = Guid.Empty;
            this.IsDirty = false;
        }

        #region Read/Write page

        /// <summary>
        /// Write a page to byte array
        /// </summary>
        public byte[] WritePage()
        {
            var writer = new ByteWriter(BasePage.PAGE_SIZE);

            this.WriteHeader(writer);
            this.WriteContent(writer);

            return writer.Buffer;
        }

        private void ReadHeader(ByteReader reader)
        {
            // first 5 bytes (pageID + pageType) was read before class create
            // this.PageID
            // this.PageType

            this.PrevPageID = reader.ReadUInt32();
            this.NextPageID = reader.ReadUInt32();
            this.ItemCount = reader.ReadUInt16();
            this.FreeBytes = reader.ReadUInt16();
            this.TransactionID = reader.ReadGuid();

            reader.Skip(59); // reserved 59 bytes
        }

        private void WriteHeader(ByteWriter writer)
        {
            writer.Write(this.PageID);
            writer.Write((byte)this.PageType);

            writer.Write(this.PrevPageID);
            writer.Write(this.NextPageID);
            writer.Write((UInt16)this.ItemCount);
            writer.Write((UInt16)this.FreeBytes);
            writer.Write(this.TransactionID);

            writer.Skip(59); // reserved 59 bytes
        }

        protected abstract void ReadContent(ByteReader reader);

        protected abstract void WriteContent(ByteWriter writer);

        #endregion

        #region Static Helper Methods

        /// <summary>
        /// Returns a size of specified number of pages
        /// </summary>
        public static long GetPagePosition(uint pageID)
        {
            return checked((long)pageID * BasePage.PAGE_SIZE);
        }

        /// <summary>
        /// Returns a size of specified number of pages
        /// </summary>
        public static long GetPagePosition(int pageID)
        {
            if (pageID < 0) throw new ArgumentOutOfRangeException(nameof(pageID), "Could not be less than 0.");

            return BasePage.GetPagePosition((uint)pageID);
        }

        /// <summary>
        /// Create a new instance of page based on T type
        /// </summary>
        public static T CreateInstance<T>(uint pageID)
            where T : BasePage
        {
            var type = typeof(T);

            // casting using "as T" #90 / thanks @Skysper
            if (type == typeof(HeaderPage)) return new HeaderPage() as T;
            if (type == typeof(CollectionListPage)) return new CollectionListPage() as T;
            if (type == typeof(CollectionPage)) return new CollectionPage(pageID) as T;
            if (type == typeof(IndexPage)) return new IndexPage(pageID) as T;
            if (type == typeof(DataPage)) return new DataPage(pageID) as T;
            if (type == typeof(ExtendPage)) return new ExtendPage(pageID) as T;
            if (type == typeof(EmptyPage)) return new EmptyPage(pageID) as T;

            throw new Exception("Invalid base page type T");
        }

        /// <summary>
        /// Create a new instance of page based on PageType
        /// </summary>
        public static BasePage CreateInstance(uint pageID, PageType pageType)
        {
            switch (pageType)
            {
                case PageType.Collection: return new CollectionPage(pageID);
                case PageType.CollectionList: return new CollectionListPage();
                case PageType.Index: return new IndexPage(pageID);
                case PageType.Data: return new DataPage(pageID);
                case PageType.Extend: return new ExtendPage(pageID);
                case PageType.Empty: return new EmptyPage(pageID);
                // use Header as default, because header page will read fixed HEADER_INFO and validate file format (if is not valid datafile)
                default: return new HeaderPage();
            }
        }

        /// <summary>
        /// Read a page with correct instance page object. Checks for pageType
        /// </summary>
        public static BasePage ReadPage(byte[] buffer)
        {
            var reader = new ByteReader(buffer);

            var pageID = reader.ReadUInt32();
            var pageType = (PageType)reader.ReadByte();

            if (pageID == 0 && (byte)pageType > 5)
            {
                throw LiteException.InvalidDatabase();
            }

            var page = BasePage.CreateInstance(pageID, pageType);

            page.ReadHeader(reader);
            page.ReadContent(reader);

            return page;
        }

        #endregion

        /// <summary>
        /// Make clone instance of this Page - by default: convert to bytes and read again (can be optimized)
        /// </summary>
        public virtual BasePage Clone()
        {
            var buffer = this.WritePage();
            return BasePage.ReadPage(buffer);
        }

        public override string ToString()
        {
            return this.PageID.ToString().PadLeft(4, '0') + " : " + this.PageType + " (" + this.ItemCount + ")";
        }
    }
}