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
    public class ImplicitTest
    {
        [TestMethod]
        public void Implicit_Test()
        {
            int i = 10;
            long l = 20;
            ulong ul = 99;

            BsonValue bi = i;
            BsonValue bl = l;
            BsonValue bu = ul;

            Assert.IsTrue(bi.IsInt32);
            Assert.IsTrue(bl.IsInt64);
            Assert.IsTrue(bu.IsDouble);



        }
    }
}