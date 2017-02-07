using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;

namespace LiteDB.Tests
{
    public class DictListData
    {
        public int Id { get; set; }
        public Dictionary<string, List<int?>> MyDict { get; set; }
    }

    [TestClass]
    public class DictListTest
    {
        [TestMethod]
        public void DictList_Test()
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