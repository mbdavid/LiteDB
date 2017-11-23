using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB_V6
{
    internal class CollectionService
    {
        private PageService _pager;

        public CollectionService(PageService pager)
        {
            _pager = pager;
        }

        /// <summary>
        /// Get a exist collection. Returns null if not exists
        /// </summary>
        public CollectionPage Get(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var header = _pager.GetPage<HeaderPage>(0);

            uint pageID;

            if (header.CollectionPages.TryGetValue(name, out pageID))
            {
                return _pager.GetPage<CollectionPage>(pageID);
            }

            throw new Exception("Collection not found: " + name);
        }
    }
}