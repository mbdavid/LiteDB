using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;
using System.IO;

namespace LiteDB.Tests
{
    public class MultiKeyId
    {
        public string Key1 { get; set; }
        public string Key2 { get; set; }
        public string Name { get; set; }
    }

    [TestClass]
    public class VirtualIdTest
    {
        [TestMethod]
        public void VirtualId_Test()
        {
            var m = new BsonMapper();

            m.Entity<MultiKeyId>()
                .Id(x => x.Key1 + "_" + x.Key2);

            using (var db = new LiteDatabase(new MemoryStream(), m))
            {
                var col = db.GetCollection<MultiKeyId>("col");

                col.Insert(new MultiKeyId
                {
                    Key1 = "a",
                    Key2 = "b",
                    Name = "John"
                });

                var obj = col.FindById("a_b");

                Assert.IsNotNull(obj);
                Assert.AreEqual("a", obj.Key1);
                Assert.AreEqual("b", obj.Key2);
                Assert.AreEqual("John", obj.Name);

            }
        }
    }
}