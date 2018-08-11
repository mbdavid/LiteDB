using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private IEnumerable<BsonDocument> SysCols()
        {
            foreach (var name in _header.Collections.Keys)
            {
                yield return new BsonDocument
                {
                    ["name"] = name,
                    ["type"] = "user"
                };
            }

            foreach (var item in _systemCollections)
            {
                yield return new BsonDocument
                {
                    ["name"] = item.Key,
                    ["type"] = item.Value.IsFunction ? "function" : "system"
                };
            }

        }
    }
}