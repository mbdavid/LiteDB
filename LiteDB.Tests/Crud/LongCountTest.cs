using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace LiteDB.Tests
{
    [TestClass]
    public class LongCountTest
    {
        public const long TOTAL_COUNT = uint.MaxValue + 10L;

        //[TestMethod]
        public void LongCountTest_Test()
        {
            using (var db = new LiteDatabase(DB.RandomFile()))
            {
                var c = db.GetCollection("col1");

                c.Insert(GetDocs());

                Assert.AreEqual(TOTAL_COUNT, c.LongCount());
            }
        }

        public IEnumerable<BsonDocument> GetDocs()
        {
            for(long i = 0; i <= TOTAL_COUNT; i++)
            {
                yield return new BsonDocument().Add("_id", i);
            }
        }
    }
}