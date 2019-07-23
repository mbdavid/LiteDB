using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests.Document
{
    [TestClass]
    public class Case_Insensitive_Tests
    {
        [TestMethod]
        public void Get_Document_Fields_Case_Insensitive()
        {
            var doc = new BsonDocument
            {
                ["_id"] = 10,
                ["name"] = "John",
                ["Last Job This Year"] = "admin"
            };

            Assert.AreEqual(10, doc["_id"].AsInt32);
            Assert.AreEqual(10, doc["_ID"].AsInt32);
            Assert.AreEqual(10, doc["_Id"].AsInt32);

            Assert.AreEqual("John", doc["name"].AsString);
            Assert.AreEqual("John", doc["Name"].AsString);
            Assert.AreEqual("John", doc["NamE"].AsString);

            Assert.AreEqual("admin", doc["Last Job This Year"].AsString);
            Assert.AreEqual("admin", doc["last JOB this YEAR"].AsString);

            // using expr
            Assert.AreEqual("admin", BsonExpression.Create("$.['Last Job This Year']").Execute(doc).First().AsString);
            Assert.AreEqual("admin", BsonExpression.Create("$.['Last JOB THIS Year']").Execute(doc).First().AsString);

        }
    }
}