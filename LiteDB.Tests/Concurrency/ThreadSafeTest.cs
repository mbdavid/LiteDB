using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LiteDB.Tests
{
    [TestClass]
    public class ThreadSafeTest
    {
        private Random _rnd = new Random();

        [TestMethod]
        public void Thread_Test()
        {
            var dbname = DB.RandomFile();
            var N = 300; // interate counter

            // use a single instance of LiteDatabase/Collection
            var a = new LiteDatabase(dbname);
            var col = a.GetCollection("col1");
            col.EnsureIndex("name");

            // task A -> insert N documents
            var ta = Task.Factory.StartNew(() =>
            {
                for (var i = 1; i <= N; i++)
                {
                    col.Insert(this.CreateDoc(i, "-insert-"));
                }
            });

            // task B -> update N documents
            var tb = Task.Factory.StartNew(() =>
            {
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
                var i = 2;
                while (i <= N)
                {
                    // delete document after update (query before)
                    if (col.Exists(Query.And(Query.EQ("_id", i), Query.EQ("name", "-update-"))))
                    {
                        var success = col.Delete(i);
                        if (success) i++;
                    }

                    // solution without query
                    //var success = col.Delete(Query.And(Query.EQ("_id", i), Query.EQ("name", "-update-"))) > 0;
                    //if (success) i++;
                }
            });

            // task D -> upload 40 files + delete 20
            var td = Task.Factory.StartNew(() =>
            {
                for (var i = 1; i <= 40; i++)
                {
                    a.FileStorage.Upload("f" + i, this.CreateMemoryFile(20000));
                }
                for (var i = 1; i <= 20; i++)
                {
                    a.FileStorage.Delete("f" + i);
                }
            });

            // Now, test data
            Task.WaitAll(ta, tb, tc, td);

            a.Dispose();

            using (var db = new LiteDatabase(dbname))
            {
                var cl = db.GetCollection("col1");
                var doc = cl.FindById(1);

                Assert.AreEqual("-update-", doc["name"].AsString);
                Assert.AreEqual(new DateTime(2015, 1, 1), doc["date"].AsDateTime);
                Assert.AreEqual(true, doc["desc"].IsNull);
                Assert.AreEqual(cl.Count(), 1);

                Assert.AreEqual(1, cl.Count());
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