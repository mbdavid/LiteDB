using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Performance_Tests
    {
        const int N1 = 10000;
        const int N2 = 1000;

        [TestMethod]
        public void Simple_Performance_Runner()
        {
            // just a simple example to test performance speed
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                var ti = new Stopwatch();
                var tx = new Stopwatch();
                var tu = new Stopwatch();
                var td = new Stopwatch();

                ti.Start();
                db.Insert("col", GetDocs(N1));
                ti.Stop();

                tx.Start();
                db.EnsureIndex("col", "name");
                tx.Stop();

                tu.Start();
                db.Update("col", GetDocs(N1));
                tu.Stop();

                db.EnsureIndex("col", "name");

                td.Start();
                db.Delete("col", Query.All());
                td.Stop();

                Debug.WriteLine("Insert time: " + ti.ElapsedMilliseconds);
                Debug.WriteLine("EnsureIndex time: " + tx.ElapsedMilliseconds);
                Debug.WriteLine("Update time: " + tu.ElapsedMilliseconds);
                Debug.WriteLine("Delete time: " + td.ElapsedMilliseconds);
            }
        }

        private IEnumerable<BsonDocument> GetDocs(int count)
        {
            var rnd = new Random();

            for(var i = 0; i < count; i++)
            {
                yield return new BsonDocument
                {
                    { "_id", i },
                    { "name", Guid.NewGuid().ToString() },
                    { "type", rnd.Next(1, 100) },
                    { "lorem", TempFile.LoremIpsum(3, 5, 2, 3, 3) }
                };
            }
        }
    }
}
