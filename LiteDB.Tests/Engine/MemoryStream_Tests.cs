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
    public class MemoryStream_Tests
    {
        [TestMethod]
        public void Engine_Using_MemoryStream()
        {
            var mem = new MemoryStream();

            using (var db = new LiteEngine(mem))
            {
                db.Insert("col", new BsonDocument { { "_id", 1 } , { "name", "John" } });
                db.Insert("col", new BsonDocument { { "_id", 2 }, { "name", "Doe" } });
            }

            using (var db = new LiteEngine(mem))
            {
                var john = db.Find("col", Query.EQ("_id", 1)).FirstOrDefault();
                var doe = db.Find("col", Query.EQ("_id", 2)).FirstOrDefault();

                Assert.AreEqual("John", john["name"].AsString);
                Assert.AreEqual("Doe", doe["name"].AsString);
            }
        }
    }
}