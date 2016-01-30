using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    public class Base
    {
        public int Id { get; set; }
    }

    public class Derived1 : Base
    {
        public string Member1 { get; set; }
    }

    public class Derived2 : Base
    {
        public string Member2 { get; set; }
    }

    [TestClass]
    public class DerivedTypeTest
    {
        [TestMethod]
        public void DerivedType_Test()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var derived1 = new Derived1 { Id = 1, Member1 = "Derived1" };
                var derived2 = new Derived2 { Id = 2, Member2 = "Dereived2" };

                var colTyped = db.GetCollection<Base>("Collection");

                colTyped.Insert(derived1);
                colTyped.Insert(derived2);

                var colBson = db.GetCollection<BsonDocument>("Collection");

                // checks if BsonDocument contains _type
                var doc1 = colBson.FindById(1);
                var doc2 = colBson.FindById(2);

                Assert.IsTrue(doc1["_type"].AsString.Contains("Derived1"));
                Assert.IsTrue(doc2["_type"].AsString.Contains("Derived2"));

                // now, test if all document will deserialize with right type
                var d1 = colTyped.FindById(1);
                var d2 = colTyped.FindById(2);

                Assert.IsTrue(d1 is Derived1);
                Assert.IsTrue(d2 is Derived2);

            }
        }
    }
}