using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    internal class CollectionService
    {
        private PageService _pager;
        private IndexService _indexer;

        public CollectionService(PageService pager, IndexService indexer)
        {
            _pager = pager;
            _indexer = indexer;
        }

        /// <summary>
        /// Get a exist collection. Returns null if not exists
        /// </summary>
        public CollectionPage Get(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            var pages = _pager.GetSeqPages<CollectionPage>(1); // PageID 1 = Master Collection

            var col = pages.FirstOrDefault(x => x.CollectionName.Equals(name, StringComparison.InvariantCulture));

            return col;
        }

        /// <summary>
        /// Add a new collection. Check if name the not exists
        /// </summary>
        public CollectionPage Add(string name)
        {
            if(string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            if(!Regex.IsMatch(name, @"^[a-zA-Z_]\w{1,29}$")) throw new ArgumentException("Invalid collection name. Use only letters, numbers and _");

            var pages = _pager.GetSeqPages<CollectionPage>(1); // PageID 1 = Master Collection

            if (pages.FirstOrDefault(x => x.CollectionName.Equals(name, StringComparison.InvariantCulture)) != null)
            {
                throw new ArgumentException("Collection name already exists (names are case unsensitive)");
            }

            if (pages.Count() >= CollectionPage.MAX_COLLECTIONS)
                throw new LiteDBException("This database exceded max collections: " + CollectionPage.MAX_COLLECTIONS);

            var col = _pager.NewPage<CollectionPage>(pages.Last());

            col.CollectionName = name;
            col.IsDirty = true;

            // create PK index
            var pk = _indexer.CreateIndex(col.PK);

            pk.Field = "_id";
            pk.Unique = true;

            return col;
        }

        /// <summary>
        /// Get all collections
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CollectionPage> GetAll()
        {
            return _pager.GetSeqPages<CollectionPage>(1); // PageID 1 = Master Collection
        }

        /// <summary>
        /// Delete a collection page and ALL data pages + indexes pages
        /// </summary>
        public bool Drop(string name)
        {
            throw new NotImplementedException();
        }
    }
}
