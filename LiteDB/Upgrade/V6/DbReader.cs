using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LiteDB;

namespace LiteDB_V6
{
    internal class DbReader : LiteDB.IDbReader
    {
        private PageService _pager;
        private CollectionService _collections;
        private FileDiskService _disk;
        private IndexService _indexer;
        private DataService _data;

        /// <summary>
        /// Initialize database reader with database stream file and password
        /// </summary>
        public bool Initialize(Stream stream, string password)
        {
            // test if current stream is V6
            if (stream.ReadByte(25 + 27) != 6) return false;

            _disk = new FileDiskService(stream, password);
            _pager = new PageService(_disk);
            _indexer = new IndexService(_pager);
            _data = new DataService(_pager);
            _collections = new CollectionService(_pager, _indexer, _data);

            return true;
        }

        /// <summary>
        /// Get all collections names from header
        /// </summary>
        public IEnumerable<string> GetCollections()
        {
            var header = _pager.GetPage<HeaderPage>(0);

            return header.CollectionPages.Keys;
        }

        /// <summary>
        /// List all indexes keys with unique information
        /// </summary>
        public IEnumerable<KeyValuePair<string, bool>> GetIndexes(string collection)
        {
            var col = _collections.Get(collection);

            foreach(var index in col.Indexes.Where(x => x.Field != "_id" && string.IsNullOrEmpty(x.Field) == false))
            {
                yield return new KeyValuePair<string, bool>(index.Field, index.Options.Unique);
            }
        }

        /// <summary>
        /// List all documents inside an collections. Use PK to get all documents in order
        /// </summary>
        public IEnumerable<BsonDocument> GetDocuments(string collection)
        {
            var col = _collections.Get(collection);

            foreach(var node in _indexer.FindAll(col.PK, Query.Ascending))
            {
                var bytes = _data.Read(node.DataBlock);

                yield return BsonSerializer.Deserialize(bytes);
            }
        }
    }
}
