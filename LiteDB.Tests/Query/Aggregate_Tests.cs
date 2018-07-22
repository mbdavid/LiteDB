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
    public class Aggregate_Tests
    {
        private LiteEngine db;
        private BsonDocument[] person;

        [TestInitialize]
        public void Init()
        {
            db = new LiteEngine();
            person = DataGen.Person(1, 1000).ToArray();

            db.Insert("person", person);
            db.EnsureIndex("col", "age");
        }

        [TestCleanup]
        public void CleanUp()
        {
            db.Dispose();
        }

        [TestMethod]
        public void Query_Aggregate_Min_Max()
        {
            // indexed
            Assert.AreEqual(
                person.Min(x => x["age"].AsInt32),
                db.Min("person", "age").AsInt32);

            Assert.AreEqual(
                person.Max(x => x["age"].AsInt32),
                db.Max("person", "age").AsInt32);

            // full query
            Assert.AreEqual(
                person.Min(x => x["name"].AsString),
                db.Min("person", "name").AsString);

            Assert.AreEqual(
                person.Max(x => x["name"].AsString),
                db.Max("person", "name").AsString);
        }

        [TestMethod]
        public void Query_Count_With_Filter()
        {
            Assert.AreEqual(
                person.LongCount(),
                db.Count("person"));

            Assert.AreEqual(
                person.Count(x => x["age"] > 20),
                db.Count("person", "age > 20"));

            // length
            Assert.AreEqual(
                person.Count(x => x["name"].AsString.Length > 20),
                db.Count("person", "LENGTH(name) > 20"));
        }

        [TestMethod]
        public void Query_Sum_With_Filter()
        {
            // sum
            Assert.AreEqual(
                person.Sum(x => x["age"].AsInt32),
                db.Query("person").SelectAll("SUM(age)").ExecuteScalar().AsInt32);

            // with filter
            Assert.AreEqual(
                person.Where(x => x["active"].AsBoolean).Sum(x => x["age"].AsInt32),
                db.Query("person").Where("active = true").SelectAll("SUM(age)").ExecuteScalar().AsInt32);
        }

        [TestMethod]
        public void Query_Exists_With_Filter()
        {
            // using expression
            Assert.AreEqual(
                person.Any(x => x["age"].AsInt32 == 99),
                db.Exists("person", "age = 99"));
        }
    }
}