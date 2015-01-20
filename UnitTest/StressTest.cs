using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace UnitTest
{
    [TestClass]
    public class StressTest
    {
        //[TestMethod]
        public void Stress_Test()
        {

            // The most important test:
            // - Create 3 task read/write operations in same file.
            // - Insert, update, delete same documents
            // - Insert big files
            // - Delete all documents
            // - Insert a big file (use all pages)
            // - Read this big file and test md5
            var file = DB.Path(true, "stress.db");
            var rnd = new Random(DateTime.Now.Second);
            var N = 100;

            var a = new LiteEngine(file);
            var b = new LiteEngine(file);
            var c = new LiteEngine(file);
            var d = new LiteEngine(file);

            // Task A -> Insert 100 documents
            var ta = Task.Factory.StartNew(() =>
            {
                var col = a.GetCollection("col1");
                for (var i = 1; i <= N; i++)
                {
                    col.Insert(CreateDoc(i, "My String Guided " + Guid.NewGuid().ToString("n")));
                }
            });

            // Task B -> Update 100 documents
            var tb = Task.Factory.StartNew(() =>
            {
                var col = b.GetCollection("col1");
                var i = 1;

                while (i < N)
                {
                    //Task.Delay(rnd.Next(100, 500));
                    var success = col.Update(CreateDoc(i, "update value"));
                    if (success) i++;
                }
            });

            //// TasK C -> Delete 99 documents (keep only _id = 1)
            //var tc = Task.Factory.StartNew(() =>
            //{
            //    var col = c.GetCollection("col1");
            //    var i = 2;

            //    while (i < N)
            //    {
            //        Task.Delay(rnd.Next(100, 500));
            //        var success = col.Delete(i);
            //        if (success) i++;
            //    }
            //});

            // Task D -> Upload 20 files

            // Now, test data
            Task.WaitAll(ta, tb); //, tb, tc);

            using (var db = new LiteEngine(file))
            {
                var col = db.GetCollection("col1");
                Assert.AreEqual(1, col.Count());
                var doc = col.FindById(1);
                Assert.AreEqual("update value", doc["Name"].AsString);
            }


            a.Dispose();
            b.Dispose();
            c.Dispose();

        }

        private BsonDocument CreateDoc(int id, string name)
        {
            var doc = new BsonDocument();
            doc.Id = id;
            doc["Name"] = name;

            return doc;
        }

        private byte[] CreateMemoryFile(int size)
        {
            var buffer = new byte[size];

            return buffer;
        }
    }
}
