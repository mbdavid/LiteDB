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
                ti.Stop();

                tx.Start();
                db.EnsureIndex("col", "name");
                tx.Stop();

                tu.Start();
                db.Update("col", GetDocs(N1));
                tu.Stop();

                td.Start();
                db.Delete("col", Query.All());
                td.Stop();

                Debug.Print("Insert time: " + ti.ElapsedMilliseconds);
                Debug.Print("EnsureIndex time: " + tx.ElapsedMilliseconds);
                Debug.Print("Update time: " + tu.ElapsedMilliseconds);
                Debug.Print("Delete time: " + td.ElapsedMilliseconds);
            }
        }

        [TestMethod]
        public void PerformanceSingleInsert_Test()
        {
            // test performance for 1.000 documents without bulk insert
            SingleInsert(true);
            SingleInsert(false);
            // now with no instance re-use (similar to v2)
            SingleInsertNewInstance(true);
            SingleInsertNewInstance(false);
        }

        private void SingleInsert(bool journal)
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(new FileDiskService(file.Filename, journal)))
            {
                var ti = new Stopwatch();

                foreach (var doc in GetDocs(N2))
                {
                    ti.Start();
                    db.Insert("col", doc);
                    ti.Stop();
                }

                Debug.Print("Insert time (" + (journal ? "" : "no ") + "journal): " + ti.ElapsedMilliseconds);
            }
        }

        private void SingleInsertNewInstance(bool journal)
        {
            using (var file = new TempFile())
            {
                var ti = new Stopwatch();

                foreach (var doc in GetDocs(N2))
                {
                    ti.Start();
                    using (var db = new LiteEngine(new FileDiskService(file.Filename, journal)))
                    {
                        db.Insert("col", doc);
                    }
                    ti.Stop();
                }

                Debug.Print("Insert time using new instance (" + (journal ? "" : "no ") + "journal): " + ti.ElapsedMilliseconds);
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