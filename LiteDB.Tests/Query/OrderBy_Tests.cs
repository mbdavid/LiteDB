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
    public class OrderBy_Tests : Person_Tests
    {
        [TestMethod]
        public void Query_OrderBy_Using_Index()
        {
            collection.EnsureIndex(x => x.Name);

            var r0 = local
                .OrderBy(x => x.Name)
                .Select(x => new { x.Name })
                .ToArray();

            var r1 = collection.Query()
                .OrderBy(x => x.Name)
                .Select(x => new { x.Name })
                .ToArray();

            CollectionAssert.AreEqual(r0, r1);
        }

        [TestMethod]
        public void Query_OrderBy_Using_Index_Desc()
        {
            collection.EnsureIndex(x => x.Name);

            var r0 = local
                .OrderByDescending(x => x.Name)
                .Select(x => new { x.Name })
                .ToArray();

            var r1 = collection.Query()
                .OrderByDescending(x => x.Name)
                .Select(x => new { x.Name })
                .ToArray();

            CollectionAssert.AreEqual(r0, r1);
        }

        [TestMethod]
        public void Query_OrderBy_With_Func()
        {
            collection.EnsureIndex(x => x.Date.Day);

            var r0 = local
                .OrderBy(x => x.Date.Day)
                .Select(x => new { d = x.Date.Day })
                .ToArray();

            var r1 = collection.Query()
                .OrderBy(x => x.Date.Day)
                .Select(x => new { d = x.Date.Day })
                .ToArray();

            CollectionAssert.AreEqual(r0, r1);
        }

        [TestMethod]
        public void Query_OrderBy_With_Offset_Limit()
        {
            // no index

            var r0 = local
                .OrderBy(x => x.Date.Day)
                .Select(x => new { d = x.Date.Day })
                .Skip(5)
                .Take(10)
                .ToArray();

            var r1 = collection.Query()
                .OrderBy(x => x.Date.Day)
                .Offset(5)
                .Limit(10)
                .Select(x => new { d = x.Date.Day })
                .ToArray();

            CollectionAssert.AreEqual(r0, r1);
        }
    }
}