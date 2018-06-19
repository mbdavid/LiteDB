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
            foreach(var item in _dataFile.Cache.Data)
            {
                yield return this.DumpPage(item.Value, item.Key, null, false, false);
            }
        }

        private IEnumerable<BsonDocument> SysCacheWal()
        {
            foreach (var item in _wal.WalFile.Cache.Data)
            {
                yield return this.DumpPage(item.Value, item.Key, null, true, true);
            }
        }
    }
}