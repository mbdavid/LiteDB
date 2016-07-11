using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    [TestClass]
    public class LoopTest
    {
        [TestMethod]
        public void Loop_Test()
        {
#if NETFULL
            var f = DB.RandomFile();
#else
            var ms = new MemoryStream();
#endif

#if NETFULL
            using (var db = new LiteDatabase(f))
#else
            using (var db = new LiteDatabase(ms))
#endif
            {
                var col = db.GetCollection("b");

                col.Insert(new BsonDocument().Add("Number", 1));
                col.Insert(new BsonDocument().Add("Number", 2));
                col.Insert(new BsonDocument().Add("Number", 3));
                col.Insert(new BsonDocument().Add("Number", 4));
            }

#if NETFULL
            using (var db = new LiteDatabase(f))
#else
            using (var db = new LiteDatabase(ms))
#endif
            {
                var col = db.GetCollection("b");

                foreach (var doc in col.FindAll())
                {
                    doc["Name"] = "John";
                    col.Update(doc);
                }

                col.EnsureIndex("Name");
                var all = col.Find(Query.EQ("Name", "John"));

                Assert.AreEqual(4, all.Count());
            }
        }
    }
}