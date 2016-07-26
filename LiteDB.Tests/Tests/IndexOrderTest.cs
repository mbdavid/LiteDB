using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    [TestClass]
    public class IndexOrderTest : TestBase
    {
        [TestMethod]
        public void Index_Order()
        {
            using (var tmp = new TempFile())
            using (var db = new LiteDatabase(tmp.ConnectionString))
            {
                var col = db.GetCollection<BsonDocument>("order");

                col.Insert(new BsonDocument().Add("text", "D"));
                col.Insert(new BsonDocument().Add("text", "A"));
                col.Insert(new BsonDocument().Add("text", "E"));
                col.Insert(new BsonDocument().Add("text", "C"));
                col.Insert(new BsonDocument().Add("text", "B"));

                col.EnsureIndex("text");

                var asc = string.Join("",
                    col.Find(Query.All("text"))
                    .Select(x => x["text"].AsString)
                    .ToArray());

                var desc = string.Join("",
                    col.Find(Query.All("text", Query.Descending))
                    .Select(x => x["text"].AsString)
                    .ToArray());

                Assert.AreEqual("ABCDE", asc);
                Assert.AreEqual("EDCBA", desc);

                var indexes = col.GetIndexes();

                Assert.AreEqual(1, indexes.Count(x => x.Field == "text"));

            }
        }
    }
}