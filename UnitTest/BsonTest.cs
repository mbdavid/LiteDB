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
            doc.Set("Customer.Address.Street", "Av. Cacapava");

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

            var d = BsonSerializer.Deserialize(bson);

            Assert.AreEqual(d["_id"], 123);
            Assert.AreEqual(d["_id"].AsInt64, o["_id"].AsInt64);

            Assert.AreEqual(o["FirstString"].AsString, d["FirstString"].AsString);
            Assert.AreEqual(o["Date"].AsDateTime.ToString(), d["Date"].AsDateTime.ToString());
            Assert.AreEqual(o["CustomerId"].AsGuid, d["CustomerId"].AsGuid);
            Assert.AreEqual(o["MyNull"].RawValue, d["MyNull"].RawValue);
            Assert.AreEqual(o["EmptyString"].AsString, d["EmptyString"].AsString);

            Assert.AreEqual(d["maxDate"].AsDateTime, DateTime.MaxValue);
            Assert.AreEqual(d["minDate"].AsDateTime, DateTime.MinValue);

            Assert.AreEqual(o["Items"].AsArray.Count, d["Items"].AsArray.Count);
            Assert.AreEqual(o["Items"].AsArray[0].AsDocument["Unit"].AsDouble, d["Items"].AsArray[0].AsDocument["Unit"].AsDouble);
            Assert.AreEqual(o["Items"].AsArray[4].AsDateTime.ToString(), d["Items"].AsArray[4].AsDateTime.ToString());
        }
    }
}
