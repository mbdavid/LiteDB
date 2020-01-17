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
    public class LongTransTest : StressTest
    {
        public LongTransTest(string filename, bool synced = false) : 
            base(filename, synced)
        {
        }

        public override void OnInit(DbContext db)
        {
        }

        [Task(Start = 0, Repeat = 2000, Random = 0, Threads = 3)]
        public void Insert(DbContext db)
        {
        }

        public override void OnCleanUp(DbContext db)
        {
        }
    }
}
