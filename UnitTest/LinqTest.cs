using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTest
{
    [TestClass]
    public class LinqTest
    {
        [TestMethod]
        public void Linq_Test()
        {
            using (var db = new LiteEngine(DB.Path()))
            {
                var c1 = new Customer { CustomerId = Guid.NewGuid(), Name = "Mauricio", CreationDate = new DateTime(2015, 1, 1) };
                var c2 = new Customer { CustomerId = Guid.NewGuid(), Name = "Malafaia", CreationDate = new DateTime(2015, 1, 1) };
                var c3 = new Customer { CustomerId = Guid.NewGuid(), Name = "Chris", CreationDate = new DateTime(2000, 1, 1) };
                var c4 = new Customer { CustomerId = Guid.NewGuid(), Name = "Juliane", CreationDate = new DateTime(2011, 8, 11) };

                var col = db.GetCollection<Customer>("Customer");

                col.EnsureIndex(x => x.Name, true);
                col.EnsureIndex(x => x.CreationDate);

                col.Insert(new Customer[] { c1, c2, c3, c4 });

                var past = -30;

                // simple
                Assert.AreEqual(1, col.Count(x => x.Name == "Chris"));
                Assert.AreEqual(2, col.Count(x => x.CreationDate == new DateTime(2015, 1, 1)));
                Assert.AreEqual(1, col.Count(x => x.Name.StartsWith("mal")));
                Assert.AreEqual(4, col.Count(x => x.CreationDate >= DateTime.Now.AddYears(past)));
                Assert.AreEqual(c3.CustomerId, col.FindOne(x => x.CreationDate <= new DateTime(2000, 1, 1)).CustomerId);
                Assert.AreEqual("Chris", col.FindOne(x => x.Name != "Mauricio").Name);

                Assert.AreEqual(1, col.Count(x => x.Name.Equals("Mauricio")));

                // and/or
                Assert.AreEqual(1, col.Count(x => x.CreationDate == new DateTime(2015, 1, 1) && x.Name.StartsWith("Mal")));
                Assert.AreEqual(2, 
                    col.Count(x => x.CreationDate == new DateTime(2015, 1, 1) && 
                        (x.Name.StartsWith("Mal") || x.Name == "Mauricio")));
            }
        }
    }
}
