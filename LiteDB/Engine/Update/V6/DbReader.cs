using System;
using System.Collections.Generic;
using System.IO;

namespace LiteDB.Update.V6
{
    internal class DbReader
    {
        private const int PAGE_SIZE = 4096;

        private Stream _stream;


        #region DiskService

        /// <summary>
        /// Read page bytes from disk and convert to Page object
        /// </summary>
        private T ReadPageDisk<T>(uint pageID)
            where T : BasePage_v6
        {
            // position cursor in stream
            _stream.Seek(pageID * (uint)PAGE_SIZE, SeekOrigin.Begin);

            var buffer = new byte[PAGE_SIZE];

            // read bytes from stream
            _stream.Read(buffer, 0, BasePage.PAGE_SIZE);

            var reader = new ByteReader(buffer);

            // page header
            reader.ReadUInt32(); // read PageID
            var pageType = (PageType)reader.ReadByte();
            var prevPageID = reader.ReadUInt32();
            var nextPageID = reader.ReadUInt32();
            var itemCount = reader.ReadUInt16();
            reader.ReadUInt16(); // FreeBytes
            reader.Skip(8); // reserved 8 bytes

            T page;

            switch (pageType)
            {
                case PageType.Header: page = this.ReadHeaderPage(reader) as T; break;
                case PageType.Collection: page = this.ReadCollectionPage(reader) as T; break;
                //case PageType.Index: return new IndexPage(pageID);
                //case PageType.Data: return new DataPage(pageID);
                //case PageType.Extend: return new ExtendPage(pageID);
                //case PageType.Empty: return new EmptyPage(pageID);
                default: throw new Exception("Invalid pageType");
            }

            // setting page header 
            page.PageID = pageID;
            page.PageType = pageType;
            page.PrevPageID = prevPageID;
            page.NextPageID = nextPageID;
            page.ItemCount = itemCount;

            return page;
        }

        /// <summary>
        /// Read Header page from ByteReader
        /// </summary>
        private HeaderPage_v6 ReadHeaderPage(ByteReader reader)
        {
            var page = new HeaderPage_v6();

            reader.Skip(100);

            var cols = reader.ReadByte();
            for (var i = 0; i < cols; i++)
            {
                page.CollectionPages.Add(reader.ReadString(), reader.ReadUInt32());
            }

            return page;
        }

        /// <summary>
        /// Read Collection page from ByteReader
        /// </summary>
        private CollectionPage_v6 ReadCollectionPage(ByteReader reader)
        {
            var page = new CollectionPage_v6();

            page.CollectionName = reader.ReadString();
            page.DocumentCount = reader.ReadInt64();
            reader.ReadUInt32(); // FreeDataPageID

            page.Indexes = new Dictionary<string, bool>();


            return page;
        }

        #endregion

        #region PagerService

        private Dictionary<uint, BasePage_v6> _cache = new Dictionary<uint, BasePage_v6>();

        /// <summary>
        /// Read a page from cache or from disk. If cache exceed 5000 pages, clear cache
        /// </summary>
        private T GetPage<T>(uint pageID)
            where T : BasePage_v6
        {
            BasePage_v6 page;

            if(_cache.Count > 5000) _cache.Clear();

            if(_cache.TryGetValue(pageID, out page))
            {
                return (T)page;
            }

            page = _cache[pageID] = this.ReadPageDisk<T>(pageID);

            return (T)page;
        }

        /// <summary>
        /// Get all pages in sequence using NextPageID
        /// </summary>
        private IEnumerable<T> GetSeqPages<T>(uint firstPageID)
            where T : BasePage_v6
        {
            var pageID = firstPageID;

            while (pageID != uint.MaxValue)
            {
                var page = this.GetPage<T>(pageID);

                pageID = page.NextPageID;

                yield return page;
            }
        }

        #endregion
    }
}