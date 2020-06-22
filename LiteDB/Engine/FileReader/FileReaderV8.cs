using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Internal class to read all datafile documents - use only Stream - no cache system (database are modified during this read - shrink)
    /// </summary>
    internal class FileReaderV8 : IFileReader
    {
        private readonly Dictionary<string, uint> _collections;
        private readonly Stream _stream;
        private readonly byte[] _buffer = new byte[PAGE_SIZE];
        private BasePage _cachedPage = null;

        public FileReaderV8(HeaderPage header, DiskService disk)
        {
            _collections = header.GetCollections().ToDictionary(x => x.Key, x => x.Value);

            // using writer stream from pool (no need to return)
            _stream = disk.GetPool(FileOrigin.Data).Writer;
        }

        /// <summary>
        /// Read all collection based on header page
        /// </summary>
        public IEnumerable<string> GetCollections()
        {
            return _collections.Keys;
        }

        /// <summary>
        /// Read all indexes from all collection pages (except _id index)
        /// </summary>
        public IEnumerable<IndexInfo> GetIndexes(string collection)
        {
            var page = this.ReadPage<CollectionPage>(_collections[collection]);

            foreach(var index in page.GetCollectionIndexes().Where(x => x.Name != "_id"))
            {
                yield return new IndexInfo
                {
                    Collection = collection,
                    Name = index.Name,
                    Expression = index.Expression,
                    Unique = index.Unique
                };
            }
        }

        /// <summary>
        /// Read all documents from current collection with NO index use - read direct from free lists
        /// There is no document order
        /// </summary>
        public IEnumerable<BsonDocument> GetDocuments(string collection)
        {
            var colPage = this.ReadPage<CollectionPage>(_collections[collection]);

            for (var slot = 0; slot < PAGE_FREE_LIST_SLOTS; slot++)
            {
                var next = colPage.FreeDataPageList[slot];

                while (next != uint.MaxValue)
                {
                    var page = this.ReadPage<DataPage>(next);

                    foreach (var block in page.GetBlocks().ToArray())
                    {
                        using (var r = new BufferReader(this.ReadBlocks(block)))
                        {
                            var doc = r.ReadDocument(null);

                            yield return doc;
                        }
                    }

                    next = page.NextPageID;
                }
            }
        }

        /// <summary>
        /// Read page from stream - do not use cache system
        /// </summary>
        private T ReadPage<T>(uint pageID)
            where T : BasePage
        {
            var position = BasePage.GetPagePosition(pageID);

            if (_cachedPage?.PageID == pageID) return (T)_cachedPage;

            _stream.Position = position;
            _stream.Read(_buffer, 0, PAGE_SIZE);

            var buffer = new PageBuffer(_buffer, 0, 0);

            return (T)(_cachedPage = BasePage.ReadPage<T>(buffer));
        }

        /// <summary>
        /// Get all data blocks from first data block
        /// </summary>
        public IEnumerable<BufferSlice> ReadBlocks(PageAddress address)
        {
            while (address != PageAddress.Empty)
            {
                var dataPage = this.ReadPage<DataPage>(address.PageID);

                var block = dataPage.GetBlock(address.Index);

                yield return block.Buffer;

                address = block.NextBlock;
            }
        }
    }
}