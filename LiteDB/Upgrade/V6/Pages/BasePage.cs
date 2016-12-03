using System;

namespace LiteDB_V6
{
    internal enum PageType { Empty = 0, Header = 1, Collection = 2, Index = 3, Data = 4, Extend = 5 }

    internal abstract class BasePage
    {
        #region Page Constants

        /// <summary>
        /// The size of each page in disk - 4096 is NTFS default
        /// </summary>
        public const int PAGE_SIZE = 4096;

        /// <summary>
        /// This size is used bytes in header pages 17 bytes (+8 reserved to future use) = 25 bytes
        /// </summary>
        public const int PAGE_HEADER_SIZE = 25;

        /// <summary>
        /// Bytes avaiable to store data removing page header size - 4071 bytes
        /// </summary>
        public const int PAGE_AVAILABLE_BYTES = PAGE_SIZE - PAGE_HEADER_SIZE;

        #endregion Page Constants

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
        /// Used for all pages to count itens inside this page(bytes, nodes, blocks, ...) [2 bytes]
        /// Its Int32 but writes in UInt16
        /// </summary>
        public int ItemCount { get; set; }

        public BasePage(uint pageID)
        {
            this.PageID = pageID;
            this.PrevPageID = uint.MaxValue;
            this.NextPageID = uint.MaxValue;
            this.ItemCount = 0;
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
            if (type == typeof(CollectionPage)) return new CollectionPage(pageID) as T;
            if (type == typeof(IndexPage)) return new IndexPage(pageID) as T;
            if (type == typeof(DataPage)) return new DataPage(pageID) as T;
            if (type == typeof(ExtendPage)) return new ExtendPage(pageID) as T;

            throw new Exception("Invalid base page type T");
        }

        /// <summary>
        /// Create a new instance of page based on PageType
        /// </summary>
        public static BasePage CreateInstance(uint pageID, PageType pageType)
        {
            switch (pageType)
            {
                case PageType.Header: return new HeaderPage();
                case PageType.Collection: return new CollectionPage(pageID);
                case PageType.Index: return new IndexPage(pageID);
                case PageType.Data: return new DataPage(pageID);
                case PageType.Extend: return new ExtendPage(pageID);
                default: throw new Exception("Invalid pageType");
            }
        }

        /// <summary>
        /// Read a page with correct instance page object. Checks for pageType
        /// </summary>
        public static BasePage ReadPage(byte[] buffer)
        {
            var reader = new LiteDB.ByteReader(buffer);

            var pageID = reader.ReadUInt32();
            var pageType = (PageType)reader.ReadByte();
            var page = CreateInstance(pageID, pageType);

            page.ReadHeader(reader);
            page.ReadContent(reader);

            return page;
        }

        public static long GetSizeOfPages(uint pageCount)
        {
            return checked((long)pageCount * BasePage.PAGE_SIZE);
        }

        private void ReadHeader(LiteDB.ByteReader reader)
        {
            // first 5 bytes (pageID + pageType) was readed before class create
            // this.PageID
            // this.PageType

            this.PrevPageID = reader.ReadUInt32();
            this.NextPageID = reader.ReadUInt32();
            this.ItemCount = reader.ReadUInt16();
            reader.ReadUInt16(); // FreeBytes;
            reader.Skip(8); // reserved 8 bytes
        }

        protected abstract void ReadContent(LiteDB.ByteReader reader);
    }
}