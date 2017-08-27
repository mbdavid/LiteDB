using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Find_Indexes_Tests
    {
        [TestMethod, TestCategory("Engine")]
        public void Find_Index_Keys()
        {
            using (var db = new LiteEngine(new MemoryStream()))
            {
                db.Insert("col", new BsonDocument { { "Number", 1 } }, BsonType.Int32);
                db.Insert("col", new BsonDocument { { "Number", 2 } }, BsonType.Int32);
                db.Insert("col", new BsonDocument { { "Number", 3 } }, BsonType.Int32);
                db.Insert("col", new BsonDocument { { "Number", 4 } }, BsonType.Int32);
                db.Insert("col", new BsonDocument { { "Number", 5 } }, BsonType.Int32);

                db.EnsureIndex("col", "Number");

                Assert.AreEqual(5, db.FindIndex("col", Query.EQ("Number", 5)).First().AsInt32);
            }
        }
    }
}