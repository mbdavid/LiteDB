using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal class CollectionService
    {
        private PageService _pager;
        private IndexService _indexer;
        private DataService _data;
        private Logger _log;

        public CollectionService(PageService pager, IndexService indexer, DataService data, Logger log)
        {
            _pager = pager;
            _indexer = indexer;
            _data = data;
            _log = log;
        }

        /// <summary>
        /// Get a exist collection. Returns null if not exists
        /// </summary>
        public CollectionPage Get(string name)
        {
            var colList = _pager.GetPage<CollectionListPage>(1);

            var pageID = colList.GetPageID(name);

            if (pageID.HasValue)
            {
                return _pager.GetPage<CollectionPage>(pageID.Value);
            }

            return null;
        }

        /// <summary>
        /// Add a new collection. Check if name the not exists
        /// </summary>
        public CollectionPage Add(string name)
        {
            if (!CollectionPage.CollectionNamePattern.IsMatch(name)) throw LiteException.InvalidFormat(name);

            // get new collection page (marked as dirty)
            var col = _pager.NewPage<CollectionPage>();

            // get collection page list to add this new collection
            var colList = _pager.GetPage<CollectionListPage>(1);

            colList.Add(name, col.PageID);

            _pager.SetDirty(colList);

            // set name into collection page
            col.CollectionName = name;

            // create PK index with _id key
            var pk = _indexer.CreateIndex(col);

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
            var colList = _pager.GetPage<CollectionListPage>(1);

            foreach (var col in colList.GetAll())
            {
                yield return _pager.GetPage<CollectionPage>(col.Value);
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
            _pager.SetDirty(col);

            // update collection list page reference
            var colList = _pager.GetPage<CollectionListPage>(1);

            colList.Rename(oldName, newName);

            _pager.SetDirty(colList);
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
                    if (index.Slot == 0)
                    {
                        pages.Add(node.DataBlock.PageID);

                        // read datablock to check if there is any extended page
                        var block = _data.GetBlock(node.DataBlock);

                        if (block.ExtendPageID != uint.MaxValue)
                        {
                            _pager.DeletePage(block.ExtendPageID, true);
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
                _pager.DeletePage(pageID);
            }

            // get collection page List
            var colList = _pager.GetPage<CollectionListPage>(1);

            colList.Delete(col.CollectionName);

            // set header as dirty after remove
            _pager.SetDirty(colList);

            _pager.DeletePage(col.PageID);
        }
    }
}