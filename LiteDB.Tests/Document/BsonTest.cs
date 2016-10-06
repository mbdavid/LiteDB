using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests
{
    [TestClass]
    public class DocumentTest
    {
        [TestMethod]
        public void DocumentImplicitConvert_Test()
        {
            var obj = new Dictionary<string, object>()
            {
                { "int", 123 },
                { "arr", new object[] { 3.0, 2, 1, "zero", false } },
                { "doc", new Dictionary<string, object>()
                    {
                        { "a", "a" },
                        { "b", new int[] { 0 } },
                    }
                }
            };

            var doc = new BsonValue(obj);

            var json = JsonSerializer.Serialize(doc, false, true);

            Assert.AreEqual("{\"int\":123,\"arr\":[3.0,2,1,\"zero\",false],\"doc\":{\"a\":\"a\",\"b\":[0]}}", json);

        }
    }
}