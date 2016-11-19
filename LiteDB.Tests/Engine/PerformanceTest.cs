using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace LiteDB.Tests
{
    [TestClass]
    public class PerformanceTest
    {
        const int N1 = 10000;
        const int N2 = 1000;

        [TestMethod]
        public void Performance_Test()
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
                db.Commit();
                ti.Stop();

                tx.Start();
                db.EnsureIndex("col", "name");
                db.Commit();
                tx.Stop();

                tu.Start();
                db.Update("col", GetDocs(N1));
                db.Commit();
                tu.Stop();

                db.EnsureIndex("col", "name");
                db.Commit();

                td.Start();
                db.Delete("col", Query.All());
                db.Commit();
                td.Stop();

                Debug.Print("Insert time: " + ti.ElapsedMilliseconds);
                Debug.Print("EnsureIndex time: " + tx.ElapsedMilliseconds);
                Debug.Print("Update time: " + tu.ElapsedMilliseconds);
                Debug.Print("Delete time: " + td.ElapsedMilliseconds);
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