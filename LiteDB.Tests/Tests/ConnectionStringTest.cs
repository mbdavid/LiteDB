using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace LiteDB.Tests
{
    [TestClass]
    public class ConnectionStringTest
    {
        [TestMethod]
        public void ConnectionString_Test()
        {
            var cs = new ConnectionString("string=A; double=33.0; bool=true; timespan=00:00:42");

            Assert.AreEqual("A", cs.GetValue("string",""));
            Assert.AreEqual(33.0, cs.GetValue("double", 0.0));
            Assert.AreEqual(true, cs.GetValue("bool", false));
            Assert.AreEqual(TimeSpan.Parse("00:00:42"), cs.GetValue("timespan", new TimeSpan()));
        }
    }
}