using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB_V6
{
    internal class CollectionService
    {
        private PageService _pager;
        private IndexService _indexer;
        private DataService _data;

        public CollectionService(PageService pager, IndexService indexer, DataService data)
        {
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

            var header = _pager.GetPage<HeaderPage>(0);

            uint pageID;

            if (header.CollectionPages.TryGetValue(name, out pageID))
            {
                return _pager.GetPage<CollectionPage>(pageID);
            }

            return null;
        }
    }
}