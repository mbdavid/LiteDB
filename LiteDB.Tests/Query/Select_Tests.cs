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
    public class Select_Tests
    {
        private Zip[] local;

        private LiteDatabase db;
        private LiteCollection<Zip> collection;

        [TestInitialize]
        public void Init()
        {
            local = DataGen.Zip().Take(20).ToArray();

            db = new LiteDatabase(new MemoryStream());
            collection = db.GetCollection<Zip>();

            collection.EnsureIndex("city");
            collection.Insert(local);
        }

        [TestCleanup]
        public void CleanUp()
        {
            db.Dispose();
        }

        [TestMethod]
        public void Query_Select_Key_Only()
        {
            // must orderBy mem data because index will be sorted
            var r0 = local
                .Select(x => x.City)
                .OrderBy(x => x)
                .ToArray();

            // this query will not deserialize document, using only index key
            var r1 = collection.Query()
                .Select(x => x.City)
                .OrderBy(x => x)
                .ToArray();

            Assert.IsTrue(r0.SequenceEqual(r1));
        }

        [TestMethod]
        public void Query_Select_New_Document()
        {
            var r0 = local
                .Select(x => new { city = x.City.ToUpper(), lat = x.Loc[0], lng = x.Loc[1] })
                .ToArray();

            var r1 = collection.Query()
                .Select(x => new { city = x.City.ToUpper(), lat = x.Loc[0], lng = x.Loc[1] })
                .ToArray();

            foreach(var r in r0.Zip(r1, (l, r) => new { left = l, right = r }))
            {
                Assert.AreEqual(r.left.city, r.right.city);
                Assert.AreEqual(r.left.lat, r.right.lat);
                Assert.AreEqual(r.left.lng, r.right.lng);
            }
        }
    }
}