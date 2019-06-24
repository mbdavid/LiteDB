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
using LiteDB.Tests;

namespace LiteDB.Internals
{
    [TestClass]
    public class Sort_Tests
    {
        private IStreamFactory _factory = new StreamFactory(new MemoryStream(), null);

        [TestMethod]
        public void Sort_String_Asc()
        {
            var source = Enumerable.Range(0, 2000)
                .Select(x => Guid.NewGuid().ToString())
                .Select(x => new KeyValuePair<BsonValue, PageAddress>(x, PageAddress.Empty))
                .ToArray();

            using (var tempDisk = new SortDisk(_factory, 10 * 8192, false))
            using (var s = new SortService(tempDisk, Query.Ascending))
            {
                s.Insert(source);

                Assert.AreEqual(2000, s.Count);
                Assert.AreEqual(2, s.Containers.Count);

                Assert.AreEqual(1905, s.Containers.ElementAt(0).Count);
                Assert.AreEqual(95, s.Containers.ElementAt(1).Count);

                var output = s.Sort().ToArray();

                CollectionAssert.AreEqual(source.OrderBy(x => x.Key).ToArray(), output);
            }
        }

        [TestMethod]
        public void Sort_Int_Desc()
        {
            var rnd = new Random();
            var source = Enumerable.Range(0, 20000)
                .Select(x => new KeyValuePair<BsonValue, PageAddress>(rnd.Next(1, 30000), PageAddress.Empty))
                .ToArray();


            using (var tempDisk = new SortDisk(_factory, 10 * 8192, false))
            using (var s = new SortService(tempDisk, Query.Descending))
            {
                s.Insert(source);

                Assert.AreEqual(20000, s.Count);
                Assert.AreEqual(3, s.Containers.Count);

                Assert.AreEqual(8192, s.Containers.ElementAt(0).Count);
                Assert.AreEqual(8192, s.Containers.ElementAt(1).Count);
                Assert.AreEqual(3616, s.Containers.ElementAt(2).Count);

                var output = s.Sort().ToArray();

                CollectionAssert.AreEqual(source.OrderByDescending(x => x.Key).ToArray(), output);
            }
        }
    }
}