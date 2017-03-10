using System;

namespace LiteDB_V6
{
    internal enum PageType { Empty = 0, Header = 1, Collection = 2, Index = 3, Data = 4, Extend = 5 }

    internal abstract class BasePage
    {
        public const int PAGE_SIZE = 4096;

        public uint PageID { get; set; }
        public abstract PageType PageType { get; }
        public uint PrevPageID { get; set; }
        public uint NextPageID { get; set; }
        public int ItemCount { get; set; }

        public BasePage(uint pageID)
        {
            this.PageID = pageID;
            this.PrevPageID = uint.MaxValue;
            this.NextPageID = uint.MaxValue;
            this.ItemCount = 0;
        }

        /// <summary>
        /// Create a new instance of page based on PageType
        /// </summary>
        public static BasePage CreateInstance(uint pageID, PageType pageType)
        {
            switch (pageType)
            {
                case PageType.Collection: return new CollectionPage(pageID);
                case PageType.Index: return new IndexPage(pageID);
                case PageType.Data: return new DataPage(pageID);
                case PageType.Extend: return new ExtendPage(pageID);
                // use Header as default, because header page will read fixed HEADER_INFO and validate file format (if is not valid datafile)
                default: return new HeaderPage();
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