using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using LiteDB.Engine;

namespace LiteDB.Tests.Query
{
    [TestClass]
    public class Where_Tests
    {
        private LiteEngine db;
        private BsonDocument[] person;

        [TestInitialize]
        public void Init()
        {
            db = new LiteEngine();
            person = DataGen.Person().ToArray();

            db.Insert("person", person);
            db.EnsureIndex("col", "age");
        }

        [TestCleanup]
        public void CleanUp()
        {
            db.Dispose();
        }

        [TestMethod]
        public void Query_Where_With_Parameter()
        {
            var r0 = person
                .Where(x => x["state"] == "FL")
                .ToArray();

            var r1 = db.Query("person")
                .Where("state = @0", "FL")
                .ToArray();

            Assert.AreEqual(r0.Length, r1.Length);
        }

        [TestMethod]
        public void Query_Multi_Where()
        {
            var r0 = person
                .Where(x => x["age"] >= 10 && x["age"] <= 40)
                .Where(x => x["name"].AsString.StartsWith("Ge"))
                .ToArray();

            var r1 = db.Query("person")
                .Where("age BETWEEN 10 AND 40")
                .Where("name LIKE 'Ge%'")
                .ToArray();

            Assert.AreEqual(r0.Length, r1.Length);
        }

        [TestMethod]
        public void Query_Single_Where_With_And()
        {
            var r0 = person
                .Where(x => x["age"] == 25 && x["active"].AsBoolean)
                .ToArray();

            var r1 = db.Query("person")
                .Where("age = 25 AND active = true")
                .ToArray();

            Assert.AreEqual(r0.Length, r1.Length);
        }

        [TestMethod]
        public void Query_Single_Where_With_Or_And_In()
        {
            var r0 = person
                .Where(x => x["age"] == 25 || x["age"] == 26 || x["age"] == 27)
                .ToArray();

            var r1 = db.Query("person")
                .Where("age = 25 OR age = 26 OR age = 27")
                .ToArray();

            var r2 = db.Query("person")
                .Where("age IN [25, 26, 27]")
                .ToArray();

            Assert.AreEqual(r0.Length, r1.Length);
            Assert.AreEqual(r1.Length, r2.Length);
        }
    }
}