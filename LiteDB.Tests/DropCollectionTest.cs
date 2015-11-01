using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitTest
{
    [TestClass]
    public class DropCollectionTest
    {
        [TestMethod]
        public void DropCollection_Test()
        {
            using (var db = new LiteDatabase(DB.Path()))
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
