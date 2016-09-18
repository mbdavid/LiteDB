using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests
{
    [TestClass]
    public class EngineTest
    {
        [TestMethod]
        public void Engine_Insert_Test()
        {
            using (var file = new TempFile())
            {
                using (var db = new LiteEngine(file.Filename))
                {
                    db.Insert("col", new BsonDocument { { "_id", 1 } , { "name", "John" } });
                    db.Insert("col", new BsonDocument { { "_id", 2 }, { "name", "Doe" } });
                }

                using (var db = new LiteEngine(file.Filename))
                {
                    var john = db.Find("col", Query.EQ("_id", 1)).FirstOrDefault();
                    var doe = db.Find("col", Query.EQ("_id", 2)).FirstOrDefault();

                    Assert.AreEqual("John", john["name"].AsString);
                    Assert.AreEqual("Doe", doe["name"].AsString);
                }
            }
        }

        [TestMethod]
        public void Engine_Upsert_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                var doc1 = new BsonDocument { { "_id", 1 }, { "name", "John" } };
                var doc2 = new BsonDocument { { "_id", 2 }, { "name", "Doe" } };

                var u1 = db.Upsert("col", doc1); // true
                var u2 = db.Upsert("col", doc1); // false

                Assert.AreEqual(true, u1);
                Assert.AreEqual(false, u2);
            }
        }
    }
}