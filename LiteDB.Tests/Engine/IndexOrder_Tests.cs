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
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var col = db.GetCollection("col");
                var indexes = db.GetCollection("$indexes");

                col.Insert(new BsonDocument { { "text", "D" } });
                col.Insert(new BsonDocument { { "text", "A" } });
                col.Insert(new BsonDocument { { "text", "E" } });
                col.Insert(new BsonDocument { { "text", "C" } });
                col.Insert(new BsonDocument { { "text", "B" } });

                col.EnsureIndex("text");

                var asc = string.Join("", col.Query()
                    .OrderBy("text")
                    .Select("text")
                    .ToValues()
                    .Select(x => x.AsString));

                var desc = string.Join("", col.Query()
                    .OrderByDescending("text")
                    .Select("text")
                    .ToValues()
                    .Select(x => x.AsString));

                Assert.AreEqual("ABCDE", asc);
                Assert.AreEqual("EDCBA", desc);

                var rr = indexes.Query().ToList();

                Assert.AreEqual(1, indexes.Count("name = 'text'"));
            }
        }
    }
}