using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LiteDB.Tests.Concurrency
{
    [TestClass]
    public class Locker_Tests
    {
        [TestMethod]
        public void Loop_With_Update()
        {
            using (var tmp = new TempFile())
            {
                // initialize database with 4
                using (var db = new LiteEngine(tmp.Filename))
                {
                    db.Insert("col", new BsonDocument { { "Number", 1 } }, BsonType.Int32);
                    db.Insert("col", new BsonDocument { { "Number", 2 } }, BsonType.Int32);
                    db.Insert("col", new BsonDocument { { "Number", 3 } }, BsonType.Int32);
                    db.Insert("col", new BsonDocument { { "Number", 4 } }, BsonType.Int32);
                    db.Insert("col", new BsonDocument { { "Number", 5 } }, BsonType.Int32);
                }

                using (var db = new LiteEngine(tmp.Filename))
                {
                    foreach (var doc in db.Find("col", Query.All(), 0, 1000))
                    {
                        var id = doc["_id"];

                        doc["Name"] = "John";

                        // inside this update, locker must be in write
                        db.Update("col", doc);
                    }

                    db.EnsureIndex("col", "Name");
                    var all = db.Find("col", Query.EQ("Name", "John"));

                    Assert.AreEqual(5, all.Count());
                }
            }
        }
    }
}