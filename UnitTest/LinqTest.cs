using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTest
{
    public class CustomerLinq
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [TestClass]
    public class LinqTest
    {
        [TestMethod]
        public void Linq_Test()
        {
            LiteDB.BsonMapper.Global.UseLowerCaseDelimiter('_');

            using (var db = new LiteDatabase(DB.Path()))
            {
                var c1 = new CustomerLinq { Id = 1, Name = "Mauricio" };
                var c2 = new CustomerLinq { Id = 2, Name = "Malafaia" };
                var c3 = new CustomerLinq { Id = 3, Name = "Chris" };
                var c4 = new CustomerLinq { Id = 4, Name = "Juliane" };

                var col = db.GetCollection<CustomerLinq>("Customer");

                col.EnsureIndex(x => x.Name, true);

                col.Insert(new CustomerLinq[] { c1, c2, c3, c4 });

                // == !=
                Assert.AreEqual(col.Count(x => x.Id == 1), 1);
                Assert.AreEqual(col.Count(x => x.Id != 1), 3);

                // methods
                Assert.AreEqual(col.Count(x => x.Name.StartsWith("mal")), 1);
                Assert.AreEqual(col.Count(x => x.Name.Equals("Mauricio")), 1);

                // > >= < <=
                Assert.AreEqual(col.Count(x => x.Id > 3), 1);
                Assert.AreEqual(col.Count(x => x.Id >= 4), 1);
                Assert.AreEqual(col.Count(x => x.Id < 2), 1);
                Assert.AreEqual(col.Count(x => x.Id <= 1), 1);

                // and/or
                Assert.AreEqual(col.Count(x => x.Id > 0 && x.Name == "MAURICIO"), 1);
                Assert.AreEqual(col.Count(x => x.Name == "malafaia" || x.Name == "MAURICIO"), 2);
            }
        }
    }
}
