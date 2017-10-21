using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LiteDB.Tests.Mapper
{
    #region Model

    public class DictListData
    {
        public int Id { get; set; }
        public Dictionary<string, List<int?>> MyDict { get; set; }
    }

    #endregion

    [TestClass]
    public class Dictionary_Tests
    {
        [TestMethod]
        public void Nested_Dictionary()
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

        [TestMethod]
        public void Dictionary_Of_List_T()
        {
            var source = new DictListData
            {
                Id = 1,
                MyDict = new Dictionary<string, List<int?>>()
                {
                    { "one", new List<int?> { 1, null, 3, null, 5 } }
                }
            };

            var mapper = new BsonMapper();

            var doc = mapper.ToDocument(source);
            var json = doc.ToString();

            var dest = mapper.ToObject<DictListData>(doc);

            Assert.AreEqual(source.MyDict["one"][0], dest.MyDict["one"][0]);
            Assert.AreEqual(source.MyDict["one"][1], dest.MyDict["one"][1]);
            Assert.AreEqual(source.MyDict["one"][2], dest.MyDict["one"][2]);
            Assert.AreEqual(source.MyDict["one"][3], dest.MyDict["one"][3]);
            Assert.AreEqual(source.MyDict["one"][4], dest.MyDict["one"][4]);

        }
    }
}