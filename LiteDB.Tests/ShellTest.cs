using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using LiteDB.Shell;

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
            var shell = new LiteShell(db);

                shell.Run("db.col1.insert $0", new BsonDocument().Add("a", 1));
                shell.Run("db.col1.ensureIndex a");
                var doc = shell.Run("db.col1.find a = 1").AsArray[0].AsDocument;
                Assert.AreEqual(1, doc["a"].AsInt32);

                // change doc field a to 2
                doc["a"] = 2;

                shell.Run("db.col1.update $0", doc);

                doc = shell.Run("db.col1.find a = 2").AsArray[0].AsDocument;
                Assert.AreEqual(2, doc["a"].AsInt32);

                shell.Run("db.col1.delete");
                Assert.AreEqual(0, shell.Run("db.col1.count").AsInt32);

                Assert.AreEqual("col1", shell.Run("show collections").AsString);
            }
        }
    }
}