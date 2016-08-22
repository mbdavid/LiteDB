using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    [TestClass]
    public class EngineTest
    {
        [TestMethod]
        public void Insert_Test()
        {
            using (var file = new TempFile())
            using (var engine = new Engine(new FileDiskService(file.ConnectionString, new Logger(), TimeSpan.FromMinutes(1))))
            {
                engine.Insert("col1", new BsonDocument[] { new BsonDocument().Add("_id", 1).Add("name", "John") });

                engine.Insert("col1", new BsonDocument[] { new BsonDocument().Add("_id", 2).Add("name", "Doe") });

                var q = engine.Find("col1", Query.EQ("_id", 1)).ToArray();


            }


            //Assert.AreEqual("mydoc2", cstr_2.Id);
        }
    }
}