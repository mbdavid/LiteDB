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
    public class ConcurrentTest
    {
        [TestMethod]
        public void Concurrent_Test()
        {
            // The most important test:
            // - Create 3 task read/write operations in same file.
            // - Insert, update, delete same documents
            // - Insert big files
            // - Delete all documents
            // - Insert a big file (use all pages)
            // - Read this big file and test md5
            var file = DB.Path();
            var rnd = new Random(DateTime.Now.Second);
            var N = 100;

            var a = new LiteDatabase(file);
            var b = new LiteDatabase(file);
            var c = new LiteDatabase(file);
            var d = new LiteDatabase(file);

            // Task A -> Insert 100 documents
            var ta = Task.Factory.StartNew(() =>
            {
                var col = a.GetCollection("col1");
                col.EnsureIndex("name");

                for (var i = 1; i <= N; i++)
                {
                    col.Insert(this.CreateDoc(i, "My String"));
                }
            });

            // Task B -> Update 100 documents
            var tb = Task.Factory.StartNew(() =>
            {
                var col = b.GetCollection("col1");
                var i = 1;

                while (i <= N)
                {
                    var success = col.Update(this.CreateDoc(i, "update value"));
                    if (success) i++;
                }
            });

            // TasK C -> Delete 99 documents (keep only _id = 1)
            var tc = Task.Factory.StartNew(() =>
            {
                var col = c.GetCollection("col1");
                var i = 2;

                while (i <= N)
                {
                    if(col.Exists(Query.And(Query.EQ("_id", i), Query.EQ("name", "update value"))))
                    {
                        var success = col.Delete(i);
                        if (success) i++;
                    }
                }
            });

            // Task D -> Upload 20 files
            var td = Task.Factory.StartNew(() =>
            {
                for (var i = 1; i <= 20; i++)
                {
                    d.FileStorage.Upload("f" + i, this.CreateMemoryFile(1024 * 1024 * 2));
                }
            });

            // Now, test data
            Task.WaitAll(ta, tb, tc, td);

            a.Dispose();
            b.Dispose();
            c.Dispose();
            d.Dispose();

            using (var db = new LiteDatabase(file))
            {
                var col = db.GetCollection("col1");
                var doc = col.FindById(1);

                Assert.AreEqual("update value", doc["name"].AsString);
                Assert.AreEqual(1, col.Count());

                Assert.AreEqual(20, db.FileStorage.FindAll().Count());
            }
        }

        private BsonDocument CreateDoc(int id, string name)
        {
            var doc = new BsonDocument();
            doc["_id"] = id;
            doc["name"] = name;

            return doc;
        }

        private MemoryStream CreateMemoryFile(int size)
        {
            var buffer = new byte[size];

            return new MemoryStream(buffer);
        }
    }
}
