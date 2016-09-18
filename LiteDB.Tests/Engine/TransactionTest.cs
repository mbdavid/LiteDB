using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LiteDB.Tests
{
    [TestClass]
    public class TransactionTest : TestBase
    {
        [TestMethod]
        public void Transaction_Test()
        {
            using (var tmp = new TempFile())
            {
                using (var db = new LiteDatabase(tmp.ConnectionString))
                {
                    var col = db.GetCollection("col");

                    using (var t = db.BeginTrans())
                    {
                        Assert.AreEqual(0, col.Count());

                        col.Insert(new BsonDocument().Add("Number", 1));
                        col.Insert(new BsonDocument().Add("Number", 2));

                        t.Commit();
                    }

                    Assert.AreEqual(2, col.Count());

                    using (var t = db.BeginTrans())
                    {
                        col.Insert(new BsonDocument().Add("Number", 3));
                        col.Insert(new BsonDocument().Add("Number", 4));

                        Assert.AreEqual(4, col.Count());

                        t.Rollback();
                    }

                    Assert.AreEqual(2, col.Count());
                }

                using (var db = new LiteDatabase(tmp.ConnectionString))
                {
                    var col = db.GetCollection("col");
                    Assert.AreEqual(2, col.Count());
                }
            }
        }
    }
}