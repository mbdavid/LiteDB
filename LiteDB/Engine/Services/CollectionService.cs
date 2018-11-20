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
        public CollectionPage Get(string name, bool addIfNotExists)
        {
            // virtual collection
            if (name.StartsWith("$"))
            {
                throw new NotImplementedException();
                //return new CollectionPage()
            }

            // get collection pageID from header
            var pageID = _header.GetCollectionPageID(name);

            if (pageID != uint.MaxValue)
            {
                return _snapshot.GetPage<CollectionPage>(pageID);
            }
            else if (addIfNotExists)
            {
                return this.Add(name);
            }

            return null;
        }

        /// <summary>
        /// Add a new collection. Check if name the not exists. Create only in transaction page - will update header only in commit
        /// </summary>
        private CollectionPage Add(string name)
        {
            if (Encoding.UTF8.GetByteCount(name) > _header.GetAvaiableCollectionSpace()) throw LiteException.InvalidCollectionName(name, "There is no space in header for more collections");
            if (!name.IsWord()) throw LiteException.InvalidCollectionName(name, "Use only [a-Z$_]");
            if (name.StartsWith("$")) throw LiteException.InvalidCollectionName(name, "Collection can't starts with `$` (reserved for system collections)");

            // create new collection page
            var collectionPage = _snapshot.NewPage<CollectionPage>();
            var pageID = collectionPage.PageID;

            // update header page on commit
            _transPages.Commit += (h) => h.InsertCollection(name, pageID);

            // create first index (_id pk)
            var indexer = new IndexService(_snapshot, collectionPage);

            indexer.CreateIndex("_id", "$._id", true);

            return collectionPage;
        }

        /// <summary>
        /// Drop a collection - remove all data pages + indexes pages
        /// </summary>
        public void Drop(string name, TransactionService transaction)
        {
            _transPages.Commit += (h) => h.DeleteCollection(name);

            throw new NotImplementedException();
            /*
            // add all pages to delete
            var pages = new HashSet<uint>();
            var indexer = new IndexService(_snapshot);
            var data = new DataService(_snapshot);

            // search for all data page and index page
            foreach (var index in col.GetIndexes(true))
            {
                // get all nodes from index
                var nodes = indexer.FindAll(index, Query.Ascending);

                foreach (var node in nodes)
                {
                    // if is PK index, add dataPages
                    if (index.Slot == 0)
                    {
                        pages.Add(node.DataBlock.PageID);

                        // read datablock to check if there is any extended page
                        var block = data.GetBlock(node.DataBlock);

                        if (block.ExtendPageID != uint.MaxValue)
                        {
                            _snapshot.DeletePages(block.ExtendPageID);
                        }
                    }

                    // add index page to delete list page
                    pages.Add(node.Position.PageID);
                }

                // remove head+tail nodes in all indexes
                pages.Add(index.HeadNode.PageID);
                pages.Add(index.TailNode.PageID);
            }

            // and now, lets delete all this pages
            foreach (var pageID in pages)
            {
                // call safe point to avoid memory leak
                transaction.Safepoint();

                // delete page
                _snapshot.DeletePage(pageID);
            }

            // mark collection page as deleted
            _snapshot.DeletePage(col.PageID);

            // otherwise, add into delete collection list
            _transPages.DeletedCollection = col.CollectionName;

            col.IsDirty = false;*/
        }
    }
}