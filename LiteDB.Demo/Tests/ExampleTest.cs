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
        public ExampleStressTest(string filename, Logger logger) : 
            base(new EngineSettings { Filename = filename }, logger)
        {
        }

        /// <summary>
        /// Use this method to initialize your stress test.
        /// You can drop existing collection, load initial data and run checkpoint before finish
        /// </summary>
        public override void OnInit(SqlDB db)
        {
            db.Insert("col1", new BsonDocument 
            { 
                ["_id"] = 1, 
                ["name"] = "John" 
            });
        }

        [Task(Start = 0, Repeat = 20, Random = 10, Threads = 4)]
        public void Insert(SqlDB db)
        {
            db.Insert("col1", new BsonDocument
            {
                ["name"] = "John " + Guid.NewGuid(),
                ["r"] = "-----------------------------------------------------------------------------------" + Guid.NewGuid(),
                ["active"] = false
            });
        }

        [Task(Start = 2000, Repeat = 2000, Random = 25, Threads = 1)]
        public void Update_Active(SqlDB db)
        {
            db.ExecuteScalar("UPDATE col1 SET active = true , r=null WHERE active = false", Guid.NewGuid());
        }

        [Task(Start = 5000, Repeat = 6000, Random = 500, Threads = 1)]
        public void Delete_Active(SqlDB db)
        {
            db.ExecuteScalar("DELETE col1 WHERE active = false");
        }

        [Task(Start = 100, Repeat = 75, Random = 25, Threads = 1)]
        public void QueryCount(SqlDB db)
        {
            db.Query("SELECT COUNT(*) FROM col1");
        }
    }
}
