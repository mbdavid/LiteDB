//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System.IO;
//using System.Linq;

//namespace LiteDB.Tests.Engine
//{
//    [TestClass]
//    public class DropCollection_Tests
//    {
//        [TestMethod]
//        public void DropCollection()
//        {
//            using (var db = new LiteEngine())
//            {
//                Assert.IsFalse(db.GetCollectionNames().Any(x => x == "col"));

//                db.Insert("col", new BsonDocument { { "a", 1 } });
//                Assert.IsTrue(db.GetCollectionNames().Any(x => x == "col"));

//                db.DropCollection("col");

//                Assert.IsFalse(db.GetCollectionNames().Any(x => x == "col"));
//            }
//        }
//    }
//}