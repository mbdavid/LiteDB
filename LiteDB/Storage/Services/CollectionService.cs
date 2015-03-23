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
        private CacheService _cache;
        private PageService _pager;
        private IndexService _indexer;
        private DataService _data;

        public CollectionService(CacheService cache, PageService pager, IndexService indexer, DataService data)
        {
            _cache = cache;
            _pager = pager;
            _indexer = indexer;
            _data = data;
        }

        /// <summary>
        /// Get a exist collection. Returns null if not exists
        /// </summary>
        public CollectionPage Get(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            if (_cache.Header.FirstCollectionPageID == uint.MaxValue) return null;

            var pages = _pager.GetSeqPages<CollectionPage>(_cache.Header.FirstCollectionPageID);

            var col = pages.FirstOrDefault(x => x.CollectionName.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            return col;
        }

        /// <summary>
        /// Add a new collection. Check if name the not exists
        /// </summary>
        public CollectionPage Add(string name)
        {
            if(string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            if(!CollectionPage.NamePattern.IsMatch(name)) throw LiteException.InvalidFormat("CollectionName", name);

            // test collection limit
            var pages = _pager.GetSeqPages<CollectionPage>(_cache.Header.FirstCollectionPageID);

            if (pages.Count() >= CollectionPage.MAX_COLLECTIONS)
            {
                throw LiteException.CollectionLimitExceeded(CollectionPage.MAX_COLLECTIONS);
            }

            var col = _pager.NewPage<CollectionPage>();

            // add page in collection list
            _pager.AddOrRemoveToFreeList(true, col, _cache.Header, ref _cache.Header.FirstCollectionPageID);

            col.CollectionName = name;
            col.IsDirty = true;

            // create PK index
            var pk = _indexer.CreateIndex(col);

            pk.Field = "_id";
            pk.Options = new IndexOptions { Unique = true };

            return col;
        }

        /// <summary>
        /// Get all collections
        /// </summary>
        public IEnumerable<CollectionPage> GetAll()
        {
            return _pager.GetSeqPages<CollectionPage>(_cache.Header.FirstCollectionPageID);
        }

        /// <summary>
        /// Drop a collection - remove all data pages + indexes pages
        /// </summary>
        public void Drop(CollectionPage col)
        {
            // add all pages to delete
            var pages = new HashSet<uint>();

            // search for all data page and index page
            foreach (var index in col.GetIndexes(true))
            {
                // get all nodes from index
                var nodes = _indexer.FindAll(index, Query.Ascending);

                foreach (var node in nodes)
                {
                    // if is PK index, add dataPages
                    if(index.Slot == 0)
                    {
                        pages.Add(node.DataBlock.PageID);

                        // read datablock to check if there is any extended page
                        var block = _data.Read(node.DataBlock, false);

                        if (block.ExtendPageID != uint.MaxValue)
                        {
                            _pager.DeletePage(block.ExtendPageID, true);
                        }
                    }

                    // add index page to delete list page
                    pages.Add(node.Position.PageID);
                }
            }

            // and now, lets delete all this pages
            foreach (var pageID in pages)
            {
                _pager.DeletePage(pageID);
            }

            // remove page from collection list
            _pager.AddOrRemoveToFreeList(false, col, _cache.Header, ref _cache.Header.FirstCollectionPageID);

            _pager.DeletePage(col.PageID, false);
        }
    }
}
