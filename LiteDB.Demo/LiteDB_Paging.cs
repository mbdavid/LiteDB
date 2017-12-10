using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    class LiteDB_Paging : ITest
    {
        static string filename = Path.Combine(Path.GetTempPath(), "litedb_paging.db");

        private LiteEngine _engine = null;
        private Query _query = Query.EQ("age", 22);

        public void Init()
        {
            File.Delete(filename);

            var disk = new FileDiskService(filename, new FileOptions
            {
                FileMode = FileMode.Exclusive,
                Journal = false
            });

            _engine = new LiteEngine(disk, cacheSize: 50000);
        }

        public void Populate(IEnumerable<BsonDocument> docs)
        {
            // create indexes before
            _engine.EnsureIndex("col", "age");

            // bulk data insert
            _engine.Insert("col", docs);
        }

        /// <summary>
        /// Count result but reading all documents from database
        /// </summary>
        public long Count() => _engine.Find("col", _query).Count();

        public List<BsonDocument> Fetch(int skip, int limit)
        {
            var result = _engine.FindSort(
                "col",
                _query,
                "$.name",
                Query.Ascending,
                skip,
                limit);

            //var result = _engine.Find("col", _query)
            //    .OrderBy(x => x["name"].AsString)
            //    .Skip(skip)
            //    .Take(limit);

            return result;

        }
    }
}