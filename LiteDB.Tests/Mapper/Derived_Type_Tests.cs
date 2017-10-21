using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace LiteDB.Tests.Mapper
{
    #region Model

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

    #endregion

    [TestClass]
    public class Derived_Type_Tests
    {
        [TestMethod]
        public void Derived_Type()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var derived1 = new Derived1 { Id = 1, Member1 = "Derived1" };
                var derived2 = new Derived2 { Id = 2, Member2 = "Dereived2" };

                var colTyped = db.GetCollection<Base>("Collection");

                colTyped.Insert(derived1);
                colTyped.Insert(derived2);

                var colBson = db.GetCollection<BsonDocument>("Collection");

                var docs = colBson.FindAll().ToList();

                Assert.IsTrue(docs.Count > 0);

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