using LiteDB.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class IndexOrder_Tests
    {
        [TestMethod]
        public void Index_Order()
        {
            using (var db = new LiteEngine())
            {
                db.Insert("col", new BsonDocument { { "text", "D" } });
                db.Insert("col", new BsonDocument { { "text", "A" } });
                db.Insert("col", new BsonDocument { { "text", "E" } });
                db.Insert("col", new BsonDocument { { "text", "C" } });
                db.Insert("col", new BsonDocument { { "text", "B" } });

                db.EnsureIndex("col", "text");

                var asc = string.Join("",
                    db.Query("col").Index(Index.All("text")).ToEnumerable()
                    .Select(x => x["text"].AsString)
                    .ToArray());

                var desc = string.Join("",
                    db.Query("col").Index(Index.All("text", Query.Descending)).ToEnumerable()
                    .Select(x => x["text"].AsString)
                    .ToArray());

                Assert.AreEqual("ABCDE", asc);
                Assert.AreEqual("EDCBA", desc);

                var indexes = db.Query("$indexes").Where("collection = 'col'").ToEnumerable();

                Assert.AreEqual(1, indexes.Count(x => x["name"] == "text"));

            }
        }
    }
}