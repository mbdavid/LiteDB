using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace LiteDB.Tests
{
    [TestClass]
    public class DbVersionTest : TestBase
    {
        [TestMethod]
        public void DbVersion_Test()
        {
            var m = new MemoryStream();

            using (var db = new LiteDatabase(m))
            {
                Assert.AreEqual(0, db.DbVersion);
                db.DbVersion = 5;
            }

            using (var db = new LiteDatabase(m))
            {
                Assert.AreEqual(5, db.DbVersion);
            }
        }
    }
}