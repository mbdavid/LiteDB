using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    [TestClass]
    public class AutoIndexEngineTest
    {
        [TestMethod]
        public void AutoIndexEngine_Test()
        {
            using (var db = new LiteEngine(new MemoryStream()))
            {
                var doc = new BsonDocument
                {
                    ["name"] = "john doe",
                    ["age"] = 40
                };

                db.Insert("people", doc);

                var result = db.FindOne("people", 
                    Query.And(
                        Query.EQ("name", "john doe"), 
                        Query.EQ("age", 40)));

                Assert.AreEqual(doc["name"], result["name"]);

                var indexName = db.GetIndexes("people").FirstOrDefault(x => x.Field == "name");
                var indexAge = db.GetIndexes("people").FirstOrDefault(x => x.Field == "age");

                // indexes are not unique (by default, when using LiteEngine)
                Assert.AreEqual(false, indexName.Unique);
                Assert.AreEqual(false, indexAge.Unique);

            }
        }
    }
}