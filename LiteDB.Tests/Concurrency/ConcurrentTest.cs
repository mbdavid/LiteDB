using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LiteDB.Tests
{
    [TestClass]
    public class ConcurrentTest
    {
        private Random _rnd = new Random();

        [TestMethod]
        public void Concurrent_Test()
        {
            var dbname = DB.RandomFile();
            var N = 300; // interate counter

            var a = new LiteDatabase(dbname);
            var b = new LiteDatabase(dbname);
            var c = new LiteDatabase(dbname);
            var d = new LiteDatabase(dbname);

            // task A -> insert N documents
            var ta = Task.Factory.StartNew(() =>
            {
                var col = a.GetCollection("col1");
                col.EnsureIndex("name");

                for (var i = 1; i <= N; i++)
                {
                    col.Insert(this.CreateDoc(i, "-insert-"));
                }
            });

            // task B -> update N documents
            var tb = Task.Factory.StartNew(() =>
            {
                var col = b.GetCollection("col1");
                var i = 1;

                while (i <= N)
                {
                    var doc = this.CreateDoc(i, "-update-");
                    doc["date"] = new DateTime(2015, 1, 1);
                    doc["desc"] = null;

                    var success = col.Update(doc);
                    if (success) i++;
                }
            });

            // tasK C -> delete N-1 documents (keep only _id = 1)
            var tc = Task.Factory.StartNew(() =>
            {
                var col = c.GetCollection("col1");
                var i = 2;

                while (i <= N)
                {
                    // delete document after update
                    if (col.Exists(Query.And(Query.EQ("_id", i), Query.EQ("name", "-update-"))))
                    {
                        var success = col.Delete(i);
                        if (success) i++;
                    }
                }
            });

            // task D -> upload 40 files + delete 20
            var td = Task.Factory.StartNew(() =>
            {
                for (var i = 1; i <= 40; i++)
                {
                    d.FileStorage.Upload("f" + i, this.CreateMemoryFile(20000));
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

            using (var db = new LiteDatabase(dbname))
            {
                var col = db.GetCollection("col1");
                var doc = col.FindById(1);

                Assert.AreEqual("-update-", doc["name"].AsString);
                Assert.AreEqual(new DateTime(2015, 1, 1), doc["date"].AsDateTime);
                Assert.AreEqual(true, doc["desc"].IsNull);
                Assert.AreEqual(col.Count(), 1);

                Assert.AreEqual(1, col.Count());
                Assert.AreEqual(20, db.FileStorage.FindAll().Count());
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