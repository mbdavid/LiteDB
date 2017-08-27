using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.IO.Compression;

namespace LiteDB.Tests
{
    [TestClass]
    public class BsonTest
    {
        private BsonDocument CreateDoc()
        {
            // create same object, but using BsonDocument
            var doc = new BsonDocument();
            doc["_id"] = 123;
            doc["FirstString"] = "BEGIN this string \" has \" \t and this \f \n\r END";
            doc["CustomerId"] = Guid.NewGuid();
            doc["Date"] = DateTime.Now;
            doc["MyNull"] = null;
            doc["EmptyObj"] = new BsonDocument();
            doc["EmptyString"] = "";
            doc["maxDate"] = DateTime.MaxValue;
            doc["minDate"] = DateTime.MinValue;


            doc["Items"] = new BsonArray();

            doc["Items"].AsArray.Add(new BsonDocument());
            doc["Items"].AsArray[0].AsDocument["Qtd"] = 3;
            doc["Items"].AsArray[0].AsDocument["Description"] = "Big beer package";
            doc["Items"].AsArray[0].AsDocument["Unit"] = (double)10 / (double)3;

            doc["Items"].AsArray.Add("string-one");
            doc["Items"].AsArray.Add(null);
            doc["Items"].AsArray.Add(true);
            doc["Items"].AsArray.Add(DateTime.Now);

            return doc;
        }

        [TestMethod]
        public void Bson_Test()
        {
            var o = CreateDoc();

            var bson = BsonSerializer.Serialize(o);
            var json = JsonSerializer.Serialize(o);

            var doc = BsonSerializer.Deserialize(bson);

            Assert.AreEqual(123, doc["_id"].AsInt32);
            Assert.AreEqual(o["_id"].AsInt64, doc["_id"].AsInt64);

            Assert.AreEqual(o["FirstString"].AsString, doc["FirstString"].AsString);
            Assert.AreEqual(o["Date"].AsDateTime.ToString(), doc["Date"].AsDateTime.ToString());
            Assert.AreEqual(o["CustomerId"].AsGuid, doc["CustomerId"].AsGuid);
            Assert.AreEqual(o["MyNull"].RawValue, doc["MyNull"].RawValue);
            Assert.AreEqual(o["EmptyString"].AsString, doc["EmptyString"].AsString);

            Assert.AreEqual(DateTime.MaxValue, doc["maxDate"].AsDateTime);
            Assert.AreEqual(DateTime.MinValue, doc["minDate"].AsDateTime);

            Assert.AreEqual(o["Items"].AsArray.Count, doc["Items"].AsArray.Count);
            Assert.AreEqual(o["Items"].AsArray[0].AsDocument["Unit"].AsDouble, doc["Items"].AsArray[0].AsDocument["Unit"].AsDouble);
            Assert.AreEqual(o["Items"].AsArray[4].AsDateTime.ToString(), doc["Items"].AsArray[4].AsDateTime.ToString());
        }
    }
}