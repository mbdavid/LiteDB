using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal class CollectionService
    {
        private readonly HeaderPage _header;
        private readonly Snapshot _snapshot;
        private readonly TransactionPages _transPages;

        public CollectionService(HeaderPage header, Snapshot snapshot, TransactionPages transPages)
        {
            _snapshot = snapshot;
            _header = header;
            _transPages = transPages;
        }

        /// <summary>
        /// Get collection page instance (or create a new one)
        /// </summary>
        public void Get(string name, bool addIfNotExists, ref CollectionPage collectionPage)
        {
            // get collection pageID from header
            var pageID = _header.GetCollectionPageID(name);

            if (pageID != uint.MaxValue)
            {
                collectionPage = _snapshot.GetPage<CollectionPage>(pageID);
            }
            else if (addIfNotExists)
            {
                this.Add(name, ref collectionPage);
            }
        }

        /// <summary>
        /// Add a new collection. Check if name the not exists. Create only in transaction page - will update header only in commit
        /// </summary>
        private void Add(string name, ref CollectionPage collectionPage)
        {
            if (Encoding.UTF8.GetByteCount(name) > _header.GetAvaiableCollectionSpace()) throw LiteException.InvalidCollectionName(name, "There is no space in header for more collections");
            if (!name.IsWord()) throw LiteException.InvalidCollectionName(name, "Use only [a-Z$_]");
            if (name.StartsWith("$")) throw LiteException.InvalidCollectionName(name, "Collection can't starts with `$` (reserved for system collections)");

            // create new collection page
            collectionPage = _snapshot.NewPage<CollectionPage>();
            var pageID = collectionPage.PageID;

            // insert collection name/pageID in header only in commit operation
            _transPages.Commit += (h) => h.InsertCollection(name, pageID);

            // create first index (_id pk) (must pass collectionPage because snapshot contains null in CollectionPage prop)
            var indexer = new IndexService(_snapshot);

            indexer.CreateIndex("_id", "$._id", true);
        }
    }
}