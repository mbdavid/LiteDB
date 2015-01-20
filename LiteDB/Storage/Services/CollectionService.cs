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

            var col = pages.FirstOrDefault(x => x.CollectionName.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            return col;
        }

        /// <summary>
        /// Add a new collection. Check if name the not exists
        /// </summary>
        public CollectionPage Add(string name)
        {
            if(string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            if(!Regex.IsMatch(name, CollectionPage.NAME_PATTERN)) throw new LiteException("Invalid collection name. Use only letters, numbers and _");

            var pages = _pager.GetSeqPages<CollectionPage>(1); // PageID 1 = Master Collection

            if (pages.FirstOrDefault(x => x.CollectionName.Equals(name, StringComparison.InvariantCultureIgnoreCase)) != null)
            {
                throw new ArgumentException("Collection name already exists (names are case unsensitive)");
            }

            if (pages.Count() >= CollectionPage.MAX_COLLECTIONS)
                throw new LiteException("This database exceded max collections: " + CollectionPage.MAX_COLLECTIONS);

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
        public IEnumerable<CollectionPage> GetAll()
        {
            return _pager.GetSeqPages<CollectionPage>(1); // PageID 1 = Master Collection
        }

        public void Drop(CollectionPage col)
        {
            // delete all index pages
            for (byte i = 0; i < col.Indexes.Length; i++)
            {
                var index = col.Indexes[i];

                if (!index.IsEmpty)
                {
                    _pager.DeletePage(index.HeadNode.PageID);
                }
            }

            // ajust page pointers
            if (col.PrevPageID != uint.MaxValue)
            {
                var prev = _pager.GetPage<BasePage>(col.PrevPageID);
                prev.NextPageID = col.NextPageID;
                prev.IsDirty = true;
            }
            if (col.NextPageID != uint.MaxValue)
            {
                var next = _pager.GetPage<BasePage>(col.NextPageID);
                next.PrevPageID = col.PrevPageID;
                next.IsDirty = true;
            }

            _pager.DeletePage(col.PageID, false);
        }
    }
}
