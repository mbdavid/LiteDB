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
        private const string dbpath = @"C:\Temp\pages.ldb";
        private Random rnd;

        [TestInitialize]
        public void Init()
        {
            rnd = new Random(DateTime.Now.Millisecond);

            File.Delete(dbpath);
        }

        [TestMethod]
        public void Page_PrevNext_Test()
        {
            using (var db = new LiteEngine(dbpath))
            {
                var k = 1;

                db.BeginTrans();

                this.PopulateCollection("my_collection_1", db, k);
                this.PopulateCollection("my_collection_2", db, k);
                this.PopulateCollection("my_collection_3", db, k);
                this.PopulateCollection("my_collection_4", db, k);
                //this.PopulateCollection("my_collection_3", db, 5);

                db.Commit();

                this.Verify("my_collection_1", db, k);
                this.Verify("my_collection_2", db, k);
                this.Verify("my_collection_3", db, k);
                this.Verify("my_collection_4", db, k);

                //Dump.Pages(db);

                db.GetCollection("my_collection_1").Delete(Query.All());
                db.GetCollection("my_collection_2").Delete(Query.All());
                db.GetCollection("my_collection_3").Delete(Query.All());
                db.GetCollection("my_collection_4").Delete(Query.All());

                Dump.Pages(db, "After clear");

                db.Files.Store("my/foto1.jpg", new MemoryStream(new byte[1024*50]));
            }
            using(var db = new LiteEngine(dbpath))
            {
                Dump.Pages(db, "After File");
            }

        }

        private void PopulateCollection(string name, LiteEngine db, int k)
        {
            var col = db.GetCollection(name);
            var doc = new BsonDocument();
            doc["Name"] = "John Doe";
            doc["Updates"] = 0;

            col.EnsureIndex("Updates");

            for (var j = 0; j < k; j++)
            {
                for (var i = 1; i <= 100; i++)
                {
                    doc["Id"] = Guid.NewGuid();
                    col.Insert(doc["Id"].AsGuid, doc);
                }
                for (var i = 1; i <= 20; i++)
                {
                    var d = col.FindOne(Query.EQ("Updates", 0));
                    d["Name"] = d["Name"].AsString.PadRight(rnd.Next(10, 500), '-');
                    d["Updates"] = 1;
                    col.Update(d["Id"].AsGuid, d);
                }
                for (var i = 1; i <= 30; i++)
                {
                    var d = col.FindOne(Query.EQ("Updates", 0));
                    d["Updates"] = 2;
                    col.Update(d["Id"].AsGuid, d);
                }

                col.Delete(Query.EQ("Updates", 0)); // delete 50;

                // balance: 50
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
