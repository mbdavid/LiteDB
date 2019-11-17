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
    public class ExampleStressTest : StressTest
    {
        public ExampleStressTest(string connectionString, Logger logger) : base(connectionString, logger)
        {
        }

        /// <summary>
        /// Use this method to initialize your stress test.
        /// You can drop existing collection, load initial data and run checkpoint before finish
        /// </summary>
        public override void OnInit(SqlDB db)
        {
            db.Execute("DROP COLLECTION col1");

            db.Execute("INSERT INTO col1 VALUES { _id: 1, name:'John' }");

            db.Execute("CHECKPOINT");
        }

        [Task(InitialDelay = 100, Repeat = 40, Random = 10)]
        public void Insert_Col1(SqlDB db)
        {
            db.Execute($"INSERT INTO col1:int VALUES {{ name: 'John-{Guid.NewGuid()}', active: false }}");
        }

        [Task(InitialDelay = 2000, Repeat = 100, Random = 0)]
        public void Update_Col1_Active(SqlDB db)
        {
            db.Execute("UPDATE col1 SET active = true WHERE active = false");
        }
    }
}
