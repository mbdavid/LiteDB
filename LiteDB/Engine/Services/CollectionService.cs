using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal class CollectionService
    {
        private Snapshot _snapshot;

        public CollectionService(Snapshot snapshot)
        {
            _snapshot = snapshot;
        }

        /// <summary>
        /// Get a exist collection. Returns null if not exists
        /// </summary>
        public CollectionPage Get(string name)
        {
            var colList = _snapshot.GetPage<CollectionListPage>(1);

            var pageID = colList.GetPageID(name);

            if (pageID.HasValue)
            {
                return _snapshot.GetPage<CollectionPage>(pageID.Value);
            }

            return null;
        }

        /// <summary>
        /// Add a new collection. Check if name the not exists
        /// </summary>
        public CollectionPage Add(string name)
        {
            if (!CollectionPage.CollectionNamePattern.IsMatch(name)) throw LiteException.InvalidCollectionName(name);

            // get new collection page (marked as dirty)
            var col = _snapshot.NewPage<CollectionPage>();

            // get collection page list to add this new collection
            var colList = _snapshot.GetPage<CollectionListPage>(1);

            colList.Add(name, col.PageID);

            _snapshot.SetDirty(colList);

            // set name into collection page
            col.CollectionName = name;

            // create PK index with _id key
            var pk = new IndexService(_snapshot).CreateIndex(col);

            pk.Name = "_id";
            pk.Expression = "$._id";
            pk.Unique = true;

            return col;
        }

        /// <summary>
        /// Get all collections pages
        /// </summary>
        public IEnumerable<CollectionPage> GetAll()
        {
            var colList = _snapshot.GetPage<CollectionListPage>(1);

            foreach (var col in colList.GetAll())
            {
                yield return _snapshot.GetPage<CollectionPage>(col.Value);
            }
        }

        /// <summary>
        /// Rename collection
        /// </summary>
        public void Rename(CollectionPage col, string newName)
        {
            // check if newName already exists
            if (this.GetAll().Select(x => x.CollectionName).Contains(newName, StringComparer.OrdinalIgnoreCase))
            {
                throw LiteException.AlreadyExistsCollectionName(newName);
            }

            var oldName = col.CollectionName;

            // change collection name on collectio page
            col.CollectionName = newName;

            // set collection page as dirty
            _snapshot.SetDirty(col);

            // update collection list page reference
            var colList = _snapshot.GetPage<CollectionListPage>(1);

            colList.Rename(oldName, newName);

            _snapshot.SetDirty(colList);
        }

        /// <summary>
        /// Drop a collection - remove all data pages + indexes pages
        /// </summary>
        public void Drop(CollectionPage col)
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
            }

            // get collection page List
            var colList = _snapshot.GetPage<CollectionListPage>(1);

            colList.Delete(col.CollectionName);

            // set header as dirty after remove
            _snapshot.SetDirty(colList);

            _snapshot.DeletePage(col.PageID);
        }
    }
}