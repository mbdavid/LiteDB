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
    public class GroupBy_Tests
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
        public void Query_GroupBy_State_With_Count()
        {
            var r0 = person
                .GroupBy(x => x["state"])
                .Select(x => new { State = x.Key.AsString, Count = x.Count() })
                .ToArray()
                .ToDictionary(x => x.State, x => x.Count);

            var r1 = db.Query("person")
                .GroupBy("state")
                .Select("{ state, count: COUNT($) }")
                .ToArray()
                .ToDictionary(x => x["state"].AsString, x => x["count"].AsInt32);

            // check for all states counts
            foreach (var key in r0.Keys)
            {
                Assert.AreEqual(r0[key], r1[key]);
            }
        }

        [TestMethod]
        public void Query_GroupBy_State_With_Sum()
        {
            var r0 = person
                .GroupBy(x => x["state"])
                .Select(x => new { State = x.Key.AsString, Sum = x.Sum(q => q["age"].AsInt32) })
                .ToArray()
                .ToDictionary(x => x.State, x => x.Sum);

            var r1 = db.Query("person")
                .GroupBy("state")
                .Select("{ state, sum: SUM(age) }")
                .ToArray()
                .ToDictionary(x => x["state"].AsString, x => x["sum"].AsInt32);

            // check for all states sums
            foreach (var key in r0.Keys)
            {
                Assert.AreEqual(r0[key], r1[key]);
            }
        }

        [TestMethod]
        public void Query_GroupBy_State_With_Filter_And_OrderBy()
        {
            var r0 = person
                .Where(x => x["age"] > 35)
                .GroupBy(x => x["state"])
                .Select(x => new { State = x.Key.AsString, Count = x.Count() })
                .OrderBy(x => x.State)
                .ToArray()
                .ToDictionary(x => x.State, x => x.Count);

            var r1 = db.Query("person")
                .Where("age > 35")
                .GroupBy("state")
                .Select("{ state, count: COUNT($) }")
                .OrderBy("state")
                .ToArray()
                .ToDictionary(x => x["state"].AsString, x => x["count"].AsInt32);

            // check state order
            Assert.AreEqual(string.Join(",", r0.Keys), string.Join(",", r1.Keys));

            // check for all states counts
            foreach (var key in r0.Keys)
            {
                Assert.AreEqual(r0[key], r1[key]);
            }
        }

        [TestMethod]
        public void Query_GroupBy_Func()
        {
            var r0 = person
                .GroupBy(x => x["date"].AsDateTime.Year)
                .Select(x => new { Year = x.Key, Count = x.Count() })
                .ToArray()
                .ToDictionary(x => x.Year, x => x.Count);

            var r1 = db.Query("person")
                .GroupBy("YEAR(date)")
                .Select("{ year: YEAR(date), count: COUNT($) }")
                .ToArray()
                .ToDictionary(x => x["year"].AsInt32, x => x["count"].AsInt32);

            // check for all states counts
            foreach (var key in r0.Keys)
            {
                Assert.AreEqual(r0[key], r1[key]);
            }
        }
    }
}