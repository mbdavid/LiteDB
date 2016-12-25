using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;

namespace LiteDB.Tests
{
    public class MyMaxValueClass
    {
        public int Id { get; set; }

        public UInt64 Min { get; set; }
        public UInt64 Max { get; set; }
    }

    [TestClass]
    public class MaxValueTest
    {
        [TestMethod]
        public void MaxValue_Test()
        {
            var c1 = new MyMaxValueClass
            {
                Min = UInt64.MinValue,
                Max = UInt64.MaxValue
            };

            var doc = BsonMapper.Global.ToDocument(c1);
            var json = JsonSerializer.Serialize(doc, true);
            var bson = BsonSerializer.Serialize(doc);

            var ndoc = BsonSerializer.Deserialize(bson);
            var c2 = BsonMapper.Global.ToObject<MyMaxValueClass>(ndoc);

            Assert.AreEqual(c1.Min, c2.Min);
            Assert.AreEqual(c1.Max, c2.Max);
        }
    }
}