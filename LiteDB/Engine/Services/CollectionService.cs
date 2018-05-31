using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal class CollectionService
    {
        private HeaderPage _header;
        private Snapshot _snapshot;
        private TransactionPages _transPages;

        public CollectionService(Snapshot snapshot, HeaderPage header, TransactionPages transPages)
        {
            _snapshot = snapshot;
            _header = header;
            _transPages = transPages;
        }

        /// <summary>
        /// Get a exist collection in header or in current transaction. Returns null if not exists
        /// </summary>
        public CollectionPage Get(string name)
        {
            // first, check if this a new collection (in this transaction)
            if (_transPages.NewCollections.TryGetValue(name, out var page)) return page;

            // but if collection was deleted, return null
            if (_transPages.DeletedCollections.ContainsKey(name)) return null;

            // otherwise, try get from header collection
            if (_header.Collections.TryGetValue(name, out var pageID))
            {
                return _snapshot.GetPage<CollectionPage>(pageID);
            }

            return null;
        }

        /// <summary>
        /// Add a new collection. Check if name the not exists. Create only in transaction page - will update header only in commit
        /// </summary>
        public CollectionPage Add(string name)
        {
            if (name.Length > CollectionPage.MAX_COLLECTION_NAME_LENGTH) throw LiteException.InvalidCollectionName(name, "MaxLength = " + CollectionPage.MAX_COLLECTION_NAME_LENGTH);
            if (!name.IsWord()) throw LiteException.InvalidCollectionName(name, "Use only [a-Z$_]");
            if (name.StartsWith("$")) throw LiteException.InvalidCollectionName(name, "Collection can't starts with `$` (reserved for virtual collections)");

            // test if not exists (global or local)
            if (_header.Collections.ContainsKey(name)) throw LiteException.AlreadyExistsCollectionName(name);
            if (_transPages.NewCollections.ContainsKey(name)) throw LiteException.AlreadyExistsCollectionName(name);

            // test if new collection name do not exceed max length (must re-checked on commit)
            _header.CheckCollectionsSize(new string[] { name });

            // create new collection page
            var col = _snapshot.NewPage<CollectionPage>();

            // set name into collection page
            col.CollectionName = name;

            // create PK index with _id key
            var indexer = new IndexService(_snapshot);
            var pk = indexer.CreateIndex(col);

            pk.Name = "_id";
            pk.Expression = "$._id";
            pk.Unique = true;

            // set collection page as dirty
            _snapshot.SetDirty(col);

            // add collection into transaction page
            _transPages.NewCollections.Add(name, col);

            return col;
        }

        /// <summary>
        /// Get all collections pages
        /// </summary>
        public IEnumerable<CollectionPage> GetAll()
        {
            // first, return new collections (local)
            foreach (var page in _transPages.NewCollections.Values)
            {
                yield return page;
            }

            // get all pages (global)
            foreach (var col in _header.Collections)
            {
                // exclude deleted pages
                if (_transPages.DeletedCollections.ContainsKey(col.Key)) continue;

                yield return _snapshot.GetPage<CollectionPage>(col.Value);
            }
        }

        /// <summary>
        /// Rename collection
        /// </summary>
        public void Rename(CollectionPage col, string newName)
        {
            throw new NotImplementedException();

            //// check if newName already exists
            //if (this.GetAll().Select(x => x.CollectionName).Contains(newName, StringComparer.OrdinalIgnoreCase))
            //{
            //    throw LiteException.AlreadyExistsCollectionName(newName);
            //}

            //var oldName = col.CollectionName;
            //
            //// change collection name on collectio page
            //col.CollectionName = newName;
            //
            //// set collection page as dirty
            //_snapshot.SetDirty(col);
            //
            //// update collection list page reference
            //var colList = _snapshot.GetPage<CollectionListPage>(1);
            //
            //colList.Rename(oldName, newName);
            //
            //_snapshot.SetDirty(colList);
        }

        /// <summary>
        /// Drop a collection - remove all data pages + indexes pages
        /// </summary>
        public void Drop(CollectionPage col, LiteTransaction transaction)
        {
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
                            _snapshot.DeletePage(block.ExtendPageID, true);
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
                // delete page
                _snapshot.DeletePage(pageID);

                // call safe point to avoid memory leak
                transaction.Safepoint();
            }

            // mark collection page as deleted
            _snapshot.DeletePage(col.PageID);

            // if deleted page are new page, just remove from new collection list
            if (_transPages.NewCollections.Remove(col.CollectionName) == false)
            {
                // otherwise, add into delete collection list
                _transPages.DeletedCollections.Add(col.CollectionName, col);
            }

            // remove reference of collection page from snapshot (for current transaction, there is no more this collection)
            _snapshot.CollectionPage = null;
        }
    }
}