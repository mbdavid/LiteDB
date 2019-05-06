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
    public class MergeSort_Tests
    {
        [TestMethod]
        public void MergeSort_String_Asc()
        {
            var source = Enumerable.Range(0, 2000)
                .Select(x => Guid.NewGuid().ToString())
                .Select(x => new KeyValuePair<BsonValue, PageAddress>(x, PageAddress.Empty))
                .ToArray();

            var f = new StreamFactory(new MemoryStream());

            using (var s = new MergeSortService(f, 10 * 8192, false))
            {
                var output = s.Sort(source, Query.Ascending).ToArray();

                CollectionAssert.AreEqual(source.OrderBy(x => x.Key).ToArray(), output);
            }
        }

        [TestMethod]
        public void MergeSort_Int_Desc()
        {
            var rnd = new Random();
            var source = Enumerable.Range(0, 20000)
                .Select(x => new KeyValuePair<BsonValue, PageAddress>(rnd.Next(1, 30000), PageAddress.Empty))
                .ToArray();

            var f = new StreamFactory(new MemoryStream());

            using (var s = new MergeSortService(f, 20 * 8192, false))
            {
                var output = s.Sort(source, Query.Descending).ToArray();

                CollectionAssert.AreEqual(source.OrderByDescending(x => x.Key).ToArray(), output);
            }
        }
    }
}