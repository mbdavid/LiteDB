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
            foreach (var name in this.GetCollectionNames())
            {
                yield return new BsonDocument
                {
                    ["name"] = name,
                };
            }
        }
    }
}