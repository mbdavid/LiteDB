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
    public class Select_Tests : Person_Tests
    {
        [TestMethod]
        public void Query_Select_Key_Only()
        {
            collection.EnsureIndex(x => x.Address.City);

            // must orderBy mem data because index will be sorted
            var r0 = local
                .OrderBy(x => x.Address.City)
                .Select(x => x.Address.City)
                .ToArray();

            // this query will not deserialize document, using only index key
            var r1 = collection.Query()
                .OrderBy(x => x.Address.City)
                .Select(x => x.Address.City)
                .ToArray();

            CollectionAssert.AreEqual(r0, r1);
        }

        [TestMethod]
        public void Query_Select_New_Document()
        {
            var r0 = local
                .Select(x => new { city = x.Address.City.ToUpper(), phone0 = x.Phones[0], address = new Address { Street = x.Name } })
                .ToArray();

            var r1 = collection.Query()
                .Select(x => new { city = x.Address.City.ToUpper(), phone0 = x.Phones[0], address = new Address { Street = x.Name } })
                .ToArray();

            foreach (var r in r0.Zip(r1, (l, r) => new { left = l, right = r }))
            {
                Assert.AreEqual(r.left.city, r.right.city);
                Assert.AreEqual(r.left.phone0, r.right.phone0);
                Assert.AreEqual(r.left.address.Street, r.right.address.Street);
            }
        }
    }
}