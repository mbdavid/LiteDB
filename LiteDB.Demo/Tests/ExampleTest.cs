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

        [Task(Delay = 0, Wait = 20, Random = 10, Tasks = 1)]
        public void Insert(SqlDB db)
        {
            db.Insert("col1", new BsonDocument
            {
                ["name"] = "John " + Guid.NewGuid(),
                //["r"] = "-----------------------------------------------------------------------------------" + Guid.NewGuid(),
                ["active"] = false
            });
        }

        //[Task(Delay = 2000, Wait = 2000, Random = 25, Tasks = 1)]
        public void Update_Active(SqlDB db)
        {
            //db.ExecuteScalar("UPDATE col1 SET active = true, new_name = @0 WHERE active = false", Guid.NewGuid());
            db.ExecuteScalar("UPDATE col1 SET active = true , r=null WHERE active = false", Guid.NewGuid());
        }

        [Task(Delay = 5000, Wait = 4000, Random = 500)]
        public void Delete_Active(SqlDB db)
        {
            db.ExecuteScalar("DELETE col1 WHERE active = false");
        }

        //[Task(Delay = 100, Wait = 75, Random = 25)]
        public void QueryCount(SqlDB db)
        {
            db.Query("SELECT COUNT(*) FROM col1");
        }
    }
}
