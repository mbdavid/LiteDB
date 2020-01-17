using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    public class InsertStressTest : StressTest
    {
        public InsertStressTest(string filename, bool synced = false) : 
            base(filename, synced)
        {
        }

        public override void OnInit(DbContext db)
        {
            db.Execute("CREATE INDEX idx_name ON col1(UPPER($.name))");
        }

        public override void OnCleanUp(DbContext db)
        {
            var total = db.Query("SELECT COUNT(*) AS qtd FROM col1")[0]["qtd"].AsInt32;

            Console.WriteLine("Total inserted: " + total);
        }

        [Task(Start = 0, Repeat = 0, Random = 0, Threads = 1)]
        public void Insert(DbContext db)
        {
            db.Insert("col1", db.GetDocs(this.Timer));
        }
    }
}
