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
        [TestInitialize]
        public void Init()
        {
        }

        [TestMethod]
        public void Bson_Document()
        {
            var d = new BsonDocument();
            d["Name"] = "John Doe";
            d["Phones"] = new BsonArray();
            d["Phones"].Add(new BsonObject());
            d["Phones"][0]["Type"] = "Mobile";
            d["Phones"][0]["Number"] = "+55 51 9900-5555";

            var dt = d.ConvertTo<Customer>();

            Assert.AreEqual(d["Name"].AsString, dt.Name);

            var d2 = BsonDocument.ConvertFrom(dt);

            Assert.AreEqual(d2["Name"].AsString, dt.Name);
        }

        [TestMethod]
        public void Bson_GetValueField()
        {
        }

        [TestMethod]
        public void Bson_Serialize()
        {
        }
    }
}
