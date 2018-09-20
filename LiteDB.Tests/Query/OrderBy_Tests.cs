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
    public class OrderBy_Tests
    {
        private Person[] local;

        private LiteDatabase db;
        private LiteCollection<Person> collection;

        [TestInitialize]
        public void Init()
        {
            local = DataGen.Person(1, 20).ToArray();

            db = new LiteDatabase(new MemoryStream());
            collection = db.GetCollection<Person>();

            collection.Insert(local);
            collection.EnsureIndex(x => x.Name);
        }

        [TestCleanup]
        public void CleanUp()
        {
            db.Dispose();
        }

        [TestMethod]
        public void Query_OrderBy_Using_Index()
        {
            var r0 = local
                .Select(x => new { x.Name })
                .OrderBy(x => x.Name)
                .ToArray();

            var r1 = collection.Query()
                .Select(x => new { x.Name })
                .OrderBy(x => x.Name)
                .ToArray();

            Assert.IsTrue(r0.SequenceEqual(r1));
        }

        [TestMethod]
        public void Query_OrderBy_Using_Index_Desc()
        {
            var r0 = local
                .Select(x => new { x.Name })
                .OrderByDescending(x => x.Name)
                .ToArray();

            var r1 = collection.Query()
                .Select(x => new { x.Name })
                .OrderByDescending(x => x.Name)
                .ToArray();

            Assert.IsTrue(r0.SequenceEqual(r1));
        }

        [TestMethod]
        public void Query_OrderBy_With_Func()
        {
            var r0 = local
                .Select(x => new { d = x.Date.Day })
                .OrderBy(x => x.d)
                .ToArray();

            var r1 = collection.Query()
                .Select(x => new { d = x.Date.Day })
                .OrderBy(x => x.d)
                .ToArray();

            Assert.IsTrue(r0.SequenceEqual(r1));
        }

        [TestMethod]
        public void Query_OrderBy_With_Offset_Limit()
        {
            var r0 = local
                .Select(x => new { d = x.Date.Day })
                .OrderBy(x => x.d)
                .Skip(5)
                .Take(10)
                .ToArray();

            var r1 = collection.Query()
                .Select(x => new { d = x.Date.Day })
                .OrderBy(x => x.d)
                .Offset(5)
                .Limit(10)
                .ToArray();

            Assert.IsTrue(r0.SequenceEqual(r1));
        }
    }
}