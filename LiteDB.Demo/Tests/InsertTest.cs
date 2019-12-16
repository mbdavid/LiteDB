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
        public InsertStressTest(string filename) : 
            base(new EngineSettings { Filename = filename })
        {
        }

        public override void OnInit(Database db)
        {
        }

        public override void OnCleanUp(Database db)
        {
        }

        public override void Run(TimeSpan timer)
        {
            this.Engine.EnsureIndex("col1", "idx_name", "UPPER($.name)", false);

            this.Engine.Insert("col1", this.GetDocs(timer), BsonAutoId.Int32);
        }

        private IEnumerable<BsonDocument> GetDocs(TimeSpan timer)
        {
            var end = DateTime.Now.Add(timer);
            var counter = 1;

            while (end >= DateTime.Now)
            {
                yield return new BsonDocument
                {
                    ["_id"] = counter++,
                    ["name"] = "John " + Guid.NewGuid(),
                    ["r"] = "myvalue",
                    ["t"] = 0,
                    ["active"] = false
                };
            }

            Console.WriteLine("Total inserted: " + counter);
        }
    }
}
