using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace LiteDB.Tests
{
    [TestClass]
    public class ShellTest
    {
        [TestMethod]
        public void Shell_Test()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                db.Run("db.col1.insert $0", new BsonDocument().Add("a", 1));
                db.Run("db.col1.ensureIndex a");
                var doc = db.Run("db.col1.find a = 1").AsArray[0].AsDocument;
                Assert.AreEqual(1, doc["a"].AsInt32);

                // change doc field a to 2
                doc["a"] = 2;

                db.Run("db.col1.update $0", doc);

                doc = db.Run("db.col1.find a = 2").AsArray[0].AsDocument;
                Assert.AreEqual(2, doc["a"].AsInt32);

                db.Run("db.col1.delete");
                Assert.AreEqual(0, db.Run("db.col1.count").AsInt32);

                Assert.AreEqual("col1", db.Run("show collections").AsString);
            }
        }
    }
}