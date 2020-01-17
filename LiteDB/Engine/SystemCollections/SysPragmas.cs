using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private IEnumerable<BsonDocument> SysPragmas()
        {
            foreach (var pragma in _header.Pragmas.Pragmas)
            {
                yield return new BsonDocument
                {
                    ["_id"] = pragma.Name,
                    ["value"] = pragma.Get()
                };
            }
        }
    }
}