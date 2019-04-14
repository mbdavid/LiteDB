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
                    ["uniqueID"] = page.Value.UniqueID,
                    ["state"] = page.Key,
                    ["position"] = page.Value.Position == long.MaxValue ? -1 : (int)page.Value.Position,
                    ["origin"] = page.Value.Origin.ToString(),
                    ["shareCounter"] = page.Value.ShareCounter,
                    ["timestamp"] = (int)page.Value.Timestamp,
                    ["offset"] = page.Value.Offset,
                    ["pageID"] = (int)page.Value.ReadUInt32(0),
                    ["pageType"] = ((PageType)page.Value[4]).ToString()
                };

                yield return doc;
            }
        }
    }
}