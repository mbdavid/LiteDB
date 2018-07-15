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
using System.Threading;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Analyze_Tests
    {
        [TestMethod]
        public void Analyze_Collection_Count()
        {
            var zip = DataGen.Zip().Take(100).ToArray();

            using (var db = new LiteEngine())
            {
                // init zip collection with 100 document
                db.Insert("zip", zip);

                db.EnsureIndex("zip", "city", "$.city", false);
                db.EnsureIndex("zip", "loc", "$.loc[*]", false);

                var indexes = db.Query("$indexes")
                    .ToEnumerable()
                    .ToDictionary(x => x["name"].AsString, x => x);

                // testing for just-created indexes (always be zero)
                Assert.AreEqual(0, indexes["_id"]["keyCount"].AsInt32);
                Assert.AreEqual(0, indexes["_id"]["uniqueKeyCount"].AsInt32);
                Assert.AreEqual(0, indexes["city"]["keyCount"].AsInt32);
                Assert.AreEqual(0, indexes["city"]["uniqueKeyCount"].AsInt32);
                Assert.AreEqual(0, indexes["loc"]["keyCount"].AsInt32);
                Assert.AreEqual(0, indexes["loc"]["uniqueKeyCount"].AsInt32);

                db.Analyze(new[] { "zip" });

                indexes = db.Query("$indexes")
                    .ToEnumerable()
                    .ToDictionary(x => x["name"].AsString, x => x);

                // count unique values
                var uniqueCity = new HashSet<string>(zip.Select(x => x["city"].AsString));
                var uniqueLoc = new HashSet<double>(zip.Select(x => x["loc"][0].AsDouble).Union(zip.Select(x => x["loc"][1].AsDouble)));

                Assert.AreEqual(zip.Length, indexes["_id"]["keyCount"].AsInt32);
                Assert.AreEqual(zip.Length, indexes["_id"]["uniqueKeyCount"].AsInt32);

                Assert.AreEqual(zip.Length, indexes["city"]["keyCount"].AsInt32);
                Assert.AreEqual(uniqueCity.Count, indexes["city"]["uniqueKeyCount"].AsInt32);

                Assert.AreEqual(zip.Length * 2, indexes["loc"]["keyCount"].AsInt32);
                Assert.AreEqual(uniqueLoc.Count, indexes["loc"]["uniqueKeyCount"].AsInt32);
            }
        }
    }
}