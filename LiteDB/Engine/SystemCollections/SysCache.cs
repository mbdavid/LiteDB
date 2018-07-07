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
            var collections = _header.Collections.ToDictionary(x => x.Value, x => x.Key);

            foreach (var item in _dataFile.Cache.Data)
            {
                yield return this.DumpPage(item.Value, item.Key, null, false, false, collections);
            }
        }

        private IEnumerable<BsonDocument> SysCacheWal()
        {
            var collections = _header.Collections.ToDictionary(x => x.Value, x => x.Key);

            foreach (var item in _wal.WalFile.Cache.Data)
            {
                yield return this.DumpPage(item.Value, item.Key, null, true, true, collections);
            }
        }
    }
}