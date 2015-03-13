using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTest
{
    [TestClass]
    public class IndexOrderTest
    {
        [TestMethod]
        public void Index_Order()
        {
            using (var db = new LiteDatabase(DB.Path()))
            {
                var col = db.GetCollection<BsonDocument>("order");

                col.Insert(new BsonDocument().Add("_id", 1).Add("text", "D"));
                col.Insert(new BsonDocument().Add("_id", 2).Add("text", "A"));
                col.Insert(new BsonDocument().Add("_id", 3).Add("text", "E"));
                col.Insert(new BsonDocument().Add("_id", 4).Add("text", "C"));
                col.Insert(new BsonDocument().Add("_id", 5).Add("text", "B"));

                col.EnsureIndex("text");

                var asc = string.Join("",
                    col.Find(Query.GTE("text", "A", Query.Ascending))
                    .Select(x => x["text"].AsString)
                    .ToArray());

                var desc = string.Join("",
                    col.Find(Query.GTE("text", "A", Query.Descending))
                    .Select(x => x["text"].AsString)
                    .ToArray());

                Assert.AreEqual("ABCDE", asc);
                Assert.AreEqual("EDCBA", desc);
            }
        }
    }
}
