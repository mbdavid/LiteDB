using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LiteDB.Tests
{
    [TestClass]
    public class LoopTest
    {
        [TestMethod]
        public void Loop_Test()
        {
            using (var tmp = new TempFile())
            {
                using (var db = new LiteEngine(tmp.Filename))
                {
                    db.Insert("col", new BsonDocument { { "Number", 1 } });
                    db.Insert("col", new BsonDocument { { "Number", 2 } });
                    db.Insert("col", new BsonDocument { { "Number", 3 } });
                    db.Insert("col", new BsonDocument { { "Number", 4 } });
                }

                using (var db = new LiteEngine(tmp.Filename))
                {
                    foreach (var doc in db.Find("col", Query.All()))
                    {
                        doc["Name"] = "John";
                        db.Update("col", doc);
                    }

                    db.EnsureIndex("col", "Name");
                    var all = db.Find("col", Query.EQ("Name", "John"));

                    Assert.AreEqual(4, all.Count());
                }
            }
        }
    }
}