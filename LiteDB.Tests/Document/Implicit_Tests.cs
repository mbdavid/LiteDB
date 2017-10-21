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
    public class Implicit_Tests
    {
        [TestMethod]
        public void Implicit_Convert()
        {
            int i = int.MaxValue;
            long l = long.MaxValue;
            ulong u = ulong.MaxValue;

            BsonValue bi = i;
            BsonValue bl = l;
            BsonValue bu = u;

            Assert.IsTrue(bi.IsInt32);
            Assert.IsTrue(bl.IsInt64);
            Assert.IsTrue(bu.IsDouble);

            Assert.AreEqual(i, bi.AsInt32);
            Assert.AreEqual(l, bl.AsInt64);
            Assert.AreEqual(u, bu.AsDouble);
        }
    }
}