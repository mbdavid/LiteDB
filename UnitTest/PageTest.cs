using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTest
{
    [TestClass]
    public class PageTest
    {
        private Random rnd;

        [TestInitialize]
        public void Init()
        {
            rnd = new Random(DateTime.Now.Millisecond);
        }

        [TestMethod]
        public void Page_PrevNext_Test()
        {
            using (var db = new LiteEngine(DB.Path()))
            {
                var k = 1;

                db.BeginTrans();

                this.PopulateCollection("my_collection_1", db, k);
                //this.PopulateCollection("my_collection_2", db, k);
                //this.PopulateCollection("my_collection_3", db, k);
                //this.PopulateCollection("my_collection_4", db, k);
                //this.PopulateCollection("my_collection_3", db, 5);

                db.Commit();

                this.Verify("my_collection_1", db, k);
                //this.Verify("my_collection_2", db, k);
                //this.Verify("my_collection_3", db, k);
                //this.Verify("my_collection_4", db, k);

                Dump.Pages(db);

                db.GetCollection("my_collection_1").Delete(Query.All());
                //db.GetCollection("my_collection_2").Delete(Query.All());
                //db.GetCollection("my_collection_3").Delete(Query.All());
                //db.GetCollection("my_collection_4").Delete(Query.All());

                Dump.Pages(db, "After clear");

                db.FileStorage.Upload("my/foto1.jpg", new MemoryStream(new byte[1024*50]));
            }

            using (var db = new LiteEngine(DB.Path(false)))
            {
                Dump.Pages(db, "After File");
            }

        }

        private void PopulateCollection(string name, LiteEngine db, int k)
        {
            var col = db.GetCollection(name);
            col.EnsureIndex("Updates");

            for (var j = 0; j < k; j++)
            {
                // create 100 documents with Update = 0
                for (var i = 1; i <= 100; i++)
                {
                    var doc = new BsonDocument();
                    doc.Id = Guid.NewGuid();
                    doc["Today"] = DateTime.Today;
                    doc["Name"] = "John Doe";
                    doc["Updates"] = 0;
                    col.Insert(doc);
                }

                // change 20 documents do Update = 1
                for (var i = 1; i <= 20; i++)
                {
                    var doc = col.FindOne(Query.EQ("Updates", 0));
                    doc["Name"] = doc["Name"].AsString.PadRight(rnd.Next(10, 500), '-');
                    doc["Updates"] = 1;
                    col.Update(doc);
                }

                // change 30 documents (with Update = 0) to Update = 2
                for (var i = 1; i <= 30; i++)
                {
                    var doc = col.FindOne(Query.EQ("Updates", 0));
                    doc["Updates"] = 2;
                    col.Update(doc);
                }

                // delete all documents with Update = 0 (50 documents)
                var deleted = col.Delete(Query.EQ("Updates", 0));

                Assert.AreEqual(50, deleted);

                // balance: 50 (20 with Update = 1, 30 with Update = 2)
            }

        }

        private void Verify(string name, LiteEngine db, int k)
        {
            var col = db.GetCollection(name);

            Assert.AreEqual(50 * k, col.Count());
            Assert.AreEqual(0 * k, col.Count(Query.EQ("Updates", 0)));
            Assert.AreEqual(20 * k, col.Count(Query.EQ("Updates", 1)));
            Assert.AreEqual(30 * k, col.Count(Query.EQ("Updates", 2)));
        }
    }
}
