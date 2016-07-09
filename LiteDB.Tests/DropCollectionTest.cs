using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace LiteDB.Tests
{
    [TestClass]
    public class DropCollectionTest
    {
        [TestMethod]
        public void DropCollection_Test()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                Assert.IsFalse(db.CollectionExists("customerCollection"));
                var collection = db.GetCollection<Customer>("customerCollection");

                collection.Insert(new Customer());
                Assert.IsTrue(db.CollectionExists("customerCollection"));

                db.DropCollection("customerCollection");
                Assert.IsFalse(db.CollectionExists("customerCollection"));
            }
        }
    }
}