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

            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var col = db.GetCollection<Zip>();

                // init zip collection with 100 document
                col.Insert(zip);

                col.EnsureIndex(x => x.City);
                col.EnsureIndex(x => x.Loc);

                var indexes = db.GetCollection("$indexes").FindAll()
                    .ToDictionary(x => x["name"].AsString, x => x, StringComparer.OrdinalIgnoreCase);

                // testing for just-created indexes (always be zero)
                Assert.AreEqual(0, indexes["_id"]["keyCount"].AsInt32);
                Assert.AreEqual(0, indexes["_id"]["uniqueKeyCount"].AsInt32);
                Assert.AreEqual(0, indexes["city"]["keyCount"].AsInt32);
                Assert.AreEqual(0, indexes["city"]["uniqueKeyCount"].AsInt32);
                Assert.AreEqual(0, indexes["loc"]["keyCount"].AsInt32);
                Assert.AreEqual(0, indexes["loc"]["uniqueKeyCount"].AsInt32);

                db.Analyze("zip");

                indexes = db.GetCollection("$indexes").FindAll()
                    .ToDictionary(x => x["name"].AsString, x => x, StringComparer.OrdinalIgnoreCase);

                // count unique values
                var uniqueCity = new HashSet<string>(zip.Select(x => x.City));
                var uniqueLoc = new HashSet<double>(zip.Select(x => x.Loc[0]).Union(zip.Select(x => x.Loc[1])));

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