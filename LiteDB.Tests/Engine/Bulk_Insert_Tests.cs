using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Bulk_Insert_Tests
    {
        [TestMethod]
        public void Bulk_Insert_Engine()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                // let's bulk 500.000 documents
                db.InsertBulk("col", GetDocs(1, 60000));

                // and assert if all are inserted (based on collection header only)
                Assert.AreEqual(60000, db.Count("col"));

                // and now count all 
                Assert.AreEqual(60000, db.Count("col", Query.All()));
            }
        }

        private IEnumerable<BsonDocument> GetDocs(int initial, int count, int type = 1)
        {
            for (var i = initial; i < initial + count; i++)
            {
                yield return new BsonDocument
                {
                    { "_id", i },
                    { "name", Guid.NewGuid().ToString() },
                    { "first", "John" },
                    { "lorem", TempFile.LoremIpsum(3, 5, 2, 3, 3) }
                };
            }
        }
    }
}