using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    [TestClass]
    public class IndexOrderTest
    {
        [TestMethod]
        public void Index_Order()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var col = db.GetCollection<BsonDocument>("order");

                col.Insert(new BsonDocument().Add("text", "D"));
                col.Insert(new BsonDocument().Add("text", "A"));
                col.Insert(new BsonDocument().Add("text", "E"));
                col.Insert(new BsonDocument().Add("text", "C"));
                col.Insert(new BsonDocument().Add("text", "B"));

                col.EnsureIndex("text");

                var asc = string.Join("",
                    col.Find(Query.All("text", Query.Ascending))
                    .Select(x => x["text"].AsString)
                    .ToArray());

                var desc = string.Join("",
                    col.Find(Query.All("text", Query.Descending))
                    .Select(x => x["text"].AsString)
                    .ToArray());

                Assert.AreEqual(asc, "ABCDE");
                Assert.AreEqual(desc, "EDCBA");
            }
        }
    }
}