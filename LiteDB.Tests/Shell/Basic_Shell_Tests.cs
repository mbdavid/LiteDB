using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests.Shell
{
    [TestClass]
    public class Basic_Shell_Tests
    {
        [TestMethod]
        public void Basic_Shell_Commands()
        {
            using (var db = new LiteEngine(new MemoryStream()))
            {
                db.Run("db.col1.insert {a: 1}");
                db.Run("db.col1.insert {a: 2}");
                db.Run("db.col1.insert {a: 3}");
                db.Run("db.col1.ensureIndex a");

                Assert.AreEqual(1, db.Run("db.col1.find a = 1").First().AsDocument["a"].AsInt32);

                db.Run("db.col1.update a = $.a + 10, b = 2 where a = 1");

                Assert.AreEqual(11, db.Run("db.col1.find a = 11").First().AsDocument["a"].AsInt32);

                Assert.AreEqual(3, db.Count("col1"));

                // insert new data
                db.Run("db.data.insert {Text: \"Anything\", Number: 10} id:int");

                db.Run("db.data.ensureIndex Text");

                var doc = db.Run("db.data.find Text like \"A\"").First() as BsonDocument;

                Assert.AreEqual(1, doc["_id"].AsInt32);
                Assert.AreEqual("Anything", doc["Text"].AsString);
                Assert.AreEqual(10, doc["Number"].AsInt32);

            }
        }
    }
}