using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace LiteDB.Tests
{
    [TestClass]
    public class DictionaryTest
    {
        [TestMethod]
        public void Dictionary_Test()
        {
            var mapper = new BsonMapper();

            // map dictionary to bsondocument
            var dict = new Dictionary<string, object>
            {
                ["_id"] = 1,
                ["MyString"] = "This is string",
                ["Nested"] = new Dictionary<string, object>()
                {
                    ["One"] = 1,
                    ["Two"] = 2,
                    ["Nested2"] = new Dictionary<string, object>()
                    {
                        ["Last"] = true
                    }
                },
                ["Array"] = new string[] { "one", "two" }
            };

            var doc = mapper.ToDocument(dict);
            var nobj = mapper.ToObject<Dictionary<string, object>>(doc);

            Assert.AreEqual(dict["_id"], nobj["_id"]);
            Assert.AreEqual(dict["MyString"], nobj["MyString"]);
            Assert.AreEqual(((Dictionary<string, object>)dict["Nested"])["One"], ((Dictionary<string, object>)nobj["Nested"])["One"]);
            Assert.AreEqual(((string[])dict["Array"])[0], ((object[])nobj["Array"])[0].ToString());
        }
    }
}