using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace LiteDB.Tests
{
    [TestClass]
    public class BulkTest
    {
        [TestMethod]
        public void Bulk_Test()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var col = db.GetCollection("b");

                col.Insert(GetDocs());

                Assert.AreEqual(220, col.Count());
            }
        }

        private IEnumerable<BsonDocument> GetDocs()
        {
            for (var i = 0; i < 220; i++)
            {
                var doc = new BsonDocument()
                    .Add("_id", Guid.NewGuid())
                    .Add("content", DB.LoremIpsum(20, 50, 1, 2, 1));

                yield return doc;
            }
        }
    }
}