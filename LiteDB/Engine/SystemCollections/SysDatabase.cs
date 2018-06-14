using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private IEnumerable<BsonDocument> SysDatabase()
        {

            yield return new BsonDocument
            {
                //["settings"] = _settings.
                ["header"] = this.DumpPage(0, _header),
                ["currentReadVersion"] = _wal.CurrentReadVersion
            };
        }
    }
}