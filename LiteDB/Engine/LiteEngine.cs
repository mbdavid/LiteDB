using LiteDB.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// A internal class that take care of all engine data structure access - it´s basic implementation of a NoSql database
    /// Its isolated from complete solution - works on low level only
    /// </summary>
    internal partial class LiteEngine : IDisposable
    {
        #region Services instances

        private CacheService _cache;

        private IDiskService _disk;

        private PageService _pager;

        private TransactionService _transaction;

        private IndexService _indexer;

        private DataService _data;

        private CollectionService _collections;

        public LiteEngine(IDiskService disk)
        {
            _disk = disk;

            var isNew = _disk.Initialize();

            if(isNew)
            {
                _disk.WritePage(0, new HeaderPage().WritePage());
            }

            _cache = new CacheService();
            _pager = new PageService(_disk, _cache);
            _indexer = new IndexService(_pager);
            _data = new DataService(_pager);
            _collections = new CollectionService(_pager, _indexer, _data);
            _transaction = new TransactionService(_disk, _cache);
        }

        #endregion

        public BsonValue InsertDocument(string col, BsonDocument doc)
        {
            return null;
        }

        public bool UpdateDocument(string col, BsonValue id, BsonDocument doc)
        {
            return true;
        }

        public int DeleteDocuments(string col, Query query)
        {
            return 0;
        }

        public IEnumerable<BsonDocument> Find(string col, Query query, int skip = 0, int limit = int.MaxValue)
        {
            return null;
        }

        public BsonValue Min(string colName, string field)
        {
            return true;
        }

        public BsonValue Max(string colName, string field)
        {
            return true;
        }

        public int Count(string colName, Query query)
        {
            return 0;
        }

        public bool Exists(string colName, Query query)
        {
            return true;
        }

        public bool EnsureIndex(string col, string field, IndexOptions options)
        {
            return true;
        }

        public IEnumerable<BsonDocument> GetIndexes(string colName)
        {
            return null;
        }

        public bool DropIndex(string colName, string field)
        {
            return true;
        }

        public IEnumerable<string> GetCollectionNames()
        {
            return null;
        }

        public bool DropCollection(string colName)
        {
            return true;
        }

        public bool RenameCollection(string colName, string newName)
        {
            return true;
        }

        public void Dispose()
        {
            _disk.Dispose();
        }
    }
}
