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
    public class BugTrackTest : StressTest
    {
        public BugTrackTest(string filename) : 
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

            this.Engine.Insert("col1", this.GetDocs(400), BsonAutoId.Int32);

            this.Engine.DeleteMany("col1", "1=1");

            this.Engine.Insert("col1", this.GetDocs(400), BsonAutoId.Int32);

            this.Engine.DeleteMany("col1", "1=1");

            this.Engine.CheckIntegrity(Console.Out);
        }

        private IEnumerable<BsonDocument> GetDocs(int count)
        {
            for(var i = 0; i < count; i++)
            {
                yield return new BsonDocument
                {
                    ["name"] = "John " + Guid.NewGuid() + "0".PadLeft(200, '0'),
                    ["r"] = "myvalue",
                    ["t"] = 0,
                    ["active"] = false
                };
            }
        }
    }
}
