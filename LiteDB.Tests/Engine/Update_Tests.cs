using LiteDB.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Update_Tests
    {
        [TestMethod]
        public void Update_IndexNodes()
        {
            using (var db = new LiteEngine())
            {
                var doc = new BsonDocument { ["_id"] = 1, ["name"] = "Mauricio", ["phones"] = new BsonArray() { "51", "11" } };

                db.Insert("col1", doc);

                db.EnsureIndex("col1", "idx_name", "name", false);
                db.EnsureIndex("col1", "idx_phones", "phones[*]", false);

                doc["name"] = "David";
                doc["phones"] = new BsonArray() { "11", "25" };

                db.Update("col1", doc);

                doc["name"] = "John";

                db.Update("col1", doc);
            }
        }

        [TestMethod]
        public void Update_ExtendBlocks()
        {
            using (var db = new LiteEngine())
            {
                var doc = new BsonDocument { ["_id"] = 1, ["d"] = new byte[1000] };

                db.Insert("col1", doc);

                // small (same page)
                doc["d"] = new byte[300];

                db.Update("col1", doc);

                var page3 = db.GetPageLog(3);

                Assert.AreEqual(7828, page3["freeBytes"].AsInt32);

                // big (same page)
                doc["d"] = new byte[2000];

                db.Update("col1", doc);

                page3 = db.GetPageLog(3);

                Assert.AreEqual(6128, page3["freeBytes"].AsInt32);

                // big (extend page)
                doc["d"] = new byte[20000];

                db.Update("col1", doc);

                page3 = db.GetPageLog(3);
                var page4 = db.GetPageLog(4);
                var page5 = db.GetPageLog(5);

                Assert.AreEqual(0, page3["freeBytes"].AsInt32);
                Assert.AreEqual(0, page4["freeBytes"].AsInt32);
                Assert.AreEqual(4428, page5["freeBytes"].AsInt32);

                // small (shrink page)
                doc["d"] = new byte[10000];

                db.Update("col1", doc);

                page3 = db.GetPageLog(3);
                page4 = db.GetPageLog(4);
                page5 = db.GetPageLog(5);

                Assert.AreEqual(0, page3["freeBytes"].AsInt32);
                Assert.AreEqual(6278, page4["freeBytes"].AsInt32);
                Assert.AreEqual("Empty", page5["pageType"].AsString);
            }
        }
    }
}