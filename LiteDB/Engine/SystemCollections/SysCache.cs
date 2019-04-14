using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private IEnumerable<BsonDocument> SysCache()
        {
            var doc = new BsonDocument();

            doc["extendSegments"] = _disk.Cache.ExtendSegments;
            doc["freePages"] = _disk.Cache.FreePages;
            doc["pagesInUse"] = _disk.Cache.PagesInUse;
            doc["unusedPages"] = _disk.Cache.UnusedPages;


            yield return doc;
        }
    }
}