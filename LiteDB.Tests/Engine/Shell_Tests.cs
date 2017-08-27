using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests
{
    [TestClass]
    public class Shell_Tests
    {
        [TestMethod]
        public void Shell_Commands()
        {
            using (var db = new LiteEngine(new MemoryStream()))
            {
                db.Run("db.col1.insert {a: 1}");
                db.Run("db.col1.insert {a: 2}");
                db.Run("db.col1.insert {a: 3}");
                db.Run("db.col1.ensureIndex a");

                Assert.AreEqual(1, db.Run("db.col1.find a = 1").First().AsDocument["a"].AsInt32);

                Assert.AreEqual(3, db.Count("col1"));
            }
        }
    }
}