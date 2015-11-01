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
    public class JsonTest
    {
        private BsonDocument CreateDoc()
        {
            // create same object, but using BsonDocument
            var doc = new BsonDocument();
            doc["_id"] = 123;
            doc["FirstString"] = "BEGIN this string \" has \" \t and this \f \n\r END";
            doc["CustomerId"] = Guid.NewGuid();
            doc["Date"] = new DateTime(2015, 1, 1);
            doc["MyNull"] = null;
            doc["Items"] = new BsonArray();
            doc["MyObj"] = new BsonDocument();
            doc["EmptyString"] = "";
            var obj = new BsonDocument();
            obj["Qtd"] = 3;
            obj["Description"] = "Big beer package";
            obj["Unit"] = 1299.995;
            doc["Items"].AsArray.Add(obj);
            doc["Items"].AsArray.Add("string-one");
            doc["Items"].AsArray.Add(null);
            doc["Items"].AsArray.Add(true);
            doc["Items"].AsArray.Add(DateTime.Now);

            doc.Set("MyObj.IsFirstId", true);

            return doc;
        }

        [TestMethod]
        public void Json_Test()
        {
            var o = CreateDoc();

            var json = JsonSerializer.Serialize(o, true);

            var d = JsonSerializer.Deserialize(json).AsDocument;

            Assert.AreEqual(d["Date"].AsDateTime, o["Date"].AsDateTime);
            Assert.AreEqual(d["CustomerId"].AsGuid, o["CustomerId"].AsGuid);
            Assert.AreEqual(d["Items"].AsArray.Count, o["Items"].AsArray.Count);
            Assert.AreEqual(d["_id"], 123);
            Assert.AreEqual(d["_id"].AsInt64, o["_id"].AsInt64);
        }
    }
}
