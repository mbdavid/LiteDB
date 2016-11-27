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

            reader.Skip(60); // HEADER_INFO + FILE_VERSION + ChangeID + FreeEmptyPageID + LastPageID + DbVersion + Password 

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
            var page = new CollectionPage_v6 { Indexes = new Dictionary<string, bool>() };

            page.CollectionName = reader.ReadString();
            reader.ReadUInt32(); // FreeDataPageID
            page.DocumentCount = reader.ReadUInt32();
            reader.ReadString(); // _id
            page.HeadNode = reader.ReadPageAddress(); // _id headnode
            reader.Skip(15); // all index info from _id headnode

            for (var i = 1; i < 15; i++)
            {
                var field = reader.ReadString();
                reader.Skip(16); // HeadNode + TailNode + FreeIndexPageID 
                var unique = reader.ReadBoolean();
                reader.Skip(4); // IgnoreCase + TrimWhitespace + EmptyStringToNull + RemoveAccents

                if (!string.IsNullOrEmpty(field))
                {
                    page.Indexes.Add(field, unique);
                }
            }

            return page;
        }
        /// <summary>
        /// Read Index page from ByteReader
        /// </summary>
        private IndexPage_v6 ReadIndexPage(ByteReader reader, int itemCount)
        {
            var page = new IndexPage_v6 { Nodes = new Dictionary<ushort, IndexNode_v6>(itemCount) };

            for (var i = 0; i < itemCount; i++)
            {
                var index = reader.ReadUInt16();
                var levels = reader.ReadByte();

                var node = new IndexNode_v6(levels);

                node.Page = this;
                node.Position = new PageAddress(this.PageID, index);
                node.KeyLength = reader.ReadUInt16();
                node.Key = reader.ReadBsonValue(node.KeyLength);
                node.DataBlock = reader.ReadPageAddress();

                for (var j = 0; j < node.Prev.Length; j++)
                {
                    node.Prev[j] = reader.ReadPageAddress();
                    node.Next[j] = reader.ReadPageAddress();
                }

                this.Nodes.Add(node.Position.Index, node);
            }

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