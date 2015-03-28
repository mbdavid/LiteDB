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
        private Random _rnd = new Random();

        [TestMethod]
        public void Concurrent_Test()
        {
            var file = DB.Path();
            var N = 300;

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
                    var doc = this.CreateDoc(i, "update value");
                    doc["date"] = new DateTime(2015, 1, 1);
                    doc["value"] = null;

                    var success = col.Update(doc);
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

            // Task D -> Upload 40 files + delete 20
            var td = Task.Factory.StartNew(() =>
            {
                for (var i = 1; i <= 40; i++)
                {
                    d.FileStorage.Upload("f" + i, this.CreateMemoryFile(1024 * 512));
                }
                for (var i = 1; i <= 20; i++)
                {
                    d.FileStorage.Delete("f" + i);
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

                Assert.AreEqual(doc["name"].AsString, "update value");
                Assert.AreEqual(doc["date"].AsDateTime, new DateTime(2015, 1, 1));
                Assert.AreEqual(doc["value"].IsNull, true);
                Assert.AreEqual(col.Count(), 1);

                Assert.AreEqual(db.FileStorage.FindAll().Count(), 20);
            }
        }

        private BsonDocument CreateDoc(int id, string name)
        {
            var doc = new BsonDocument();

            doc["_id"] = id;
            doc["name"] = name;
            doc["desc"] = DB.LoremIpsum(10, 10, 2, 2, 2);
            doc["date"] = DateTime.Now.AddDays(_rnd.Next(300));
            doc["value"] = _rnd.NextDouble() * 5000;

            return doc;
        }

        private MemoryStream CreateMemoryFile(int size)
        {
            var buffer = new byte[size];

            return new MemoryStream(buffer);
        }
    }
}
