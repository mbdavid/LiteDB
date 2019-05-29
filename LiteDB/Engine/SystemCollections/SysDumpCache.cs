using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private IEnumerable<BsonDocument> SysDumpCache()
        {
            foreach(var page in _disk.Cache.GetPages())
            {
                var doc = new BsonDocument
                {
                    ["uniqueID"] = page.UniqueID,
                    ["position"] = page.Position == long.MaxValue ? -1 : (int)page.Position,
                    ["origin"] = page.Origin.ToString(),
                    ["shareCounter"] = page.ShareCounter,
                    ["timestamp"] = (int)page.Timestamp,
                    ["offset"] = page.Offset,
                    ["pageID"] = (int)page.ReadUInt32(BasePage.P_PAGE_ID),
                    ["pageType"] = ((PageType)page[BasePage.P_PAGE_TYPE]).ToString()
                };

                yield return doc;
            }
        }
    }
}