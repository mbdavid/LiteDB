using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace LiteDB.Tests
{
    [TestClass]
    public class AutoIndexTest
    {
        [TestMethod]
        public void AutoIndex_Test()
        {
            using (var database = new LiteDatabase(new MemoryStream()))
            {
                var doc1 = new BsonDocument { ["name"] = "john doe" };
                var people = database.GetCollection("people");
                people.Insert(doc1);
                var result = people.FindOne(Query.EQ("name", "john doe"));

                Assert.AreEqual(doc1["name"], result["name"]);
            }
        }
    }
}