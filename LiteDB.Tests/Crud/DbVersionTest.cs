using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;

namespace LiteDB.Tests
{
    [TestClass]
    public class DbVersionTest
    {
        [TestMethod]
        public void DbVerion_Test()
        {
            //TODO: implement DbVersion test
            //var m = new MemoryStream();
            //
            //using (var db = new VerDatabase(m, 1))
            //{
            //    Assert.AreEqual(true, db.CollectionExists("col1"));
            //    Assert.AreEqual(false, db.CollectionExists("col2"));
            //    Assert.AreEqual(false, db.CollectionExists("col3"));
            //}
            //
            //using (var db = new VerDatabase(m, 2))
            //{
            //    Assert.AreEqual(true, db.CollectionExists("col1"));
            //    Assert.AreEqual(true, db.CollectionExists("col2"));
            //    Assert.AreEqual(false, db.CollectionExists("col3"));
            //}
            //
            //using (var db = new VerDatabase(m, 3))
            //{
            //    Assert.AreEqual(true, db.CollectionExists("col1"));
            //    Assert.AreEqual(true, db.CollectionExists("col2"));
            //    Assert.AreEqual(true, db.CollectionExists("col3"));
            //}
        }
    }
}