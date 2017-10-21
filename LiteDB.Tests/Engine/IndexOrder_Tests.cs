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
            using (var tmp = new TempFile())
            using (var db = new LiteEngine(tmp.Filename))
            {
                db.Insert("col", new BsonDocument { { "text", "D" } });
                db.Insert("col", new BsonDocument { { "text", "A" } });
                db.Insert("col", new BsonDocument { { "text", "E" } });
                db.Insert("col", new BsonDocument { { "text", "C" } });
                db.Insert("col", new BsonDocument { { "text", "B" } });

                db.EnsureIndex("col", "text");

                var asc = string.Join("",
                    db.Find("col", Query.All("text"))
                    .Select(x => x["text"].AsString)
                    .ToArray());

                var desc = string.Join("",
                    db.Find("col", Query.All("text", Query.Descending))
                    .Select(x => x["text"].AsString)
                    .ToArray());

                Assert.AreEqual("ABCDE", asc);
                Assert.AreEqual("EDCBA", desc);

                var indexes = db.GetIndexes("col");

                Assert.AreEqual(1, indexes.Count(x => x.Field == "text"));

            }
        }
    }
}