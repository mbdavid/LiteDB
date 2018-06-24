using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using LiteDB.Engine;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Aggregate_Tests
    {
        [TestMethod]
        public void Aggregate_Query()
        {
            var data = DataGen.Person(20, 10).ToArray();

            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                db.Insert("col", data);

                var min = db.Min("col", "age");
                var max = db.Max("col", "age");

                Assert.AreEqual(8, min.AsInt32);
                Assert.AreEqual(54, max.AsInt32);

                // with index
                db.EnsureIndex("col", "age");

                var c1 = db.Count("col", Query.EQ("age", 8));
                var c2 = db.Count("col", "active = true");

                Assert.AreEqual(1, c1.AsInt32);
                Assert.AreEqual(1, c1.AsInt32);

                var s1 = db.Aggregate("col", "SUM(age)");

                Assert.AreEqual(252, s1.AsInt32);
            }
        }
    }
}