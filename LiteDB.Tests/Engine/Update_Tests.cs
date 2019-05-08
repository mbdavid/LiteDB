using LiteDB.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Update_Tests
    {
        [TestMethod]
        public void Update_IndexNodes()
        {
            using (var db = new LiteEngine())
            {
                var doc = new BsonDocument { ["_id"] = 1, ["name"] = "Mauricio", ["phones"] = new BsonArray() { "51", "11" } };

                db.Insert("col1", doc);

                db.EnsureIndex("col1", "idx_name", "name", false);
                db.EnsureIndex("col1", "idx_phones", "phones[*]", false);

                doc["name"] = "David";
                doc["phones"] = new BsonArray() { "11", "25" };

                db.Update("col1", doc);

                doc["name"] = "John";

                db.Update("col1", doc);


            }
        }
    }
}