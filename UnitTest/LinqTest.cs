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
                Assert.AreEqual(1, col.Count(x => x.Id == 1));
                Assert.AreEqual(3, col.Count(x => x.Id != 1));

                // methods
                Assert.AreEqual(1, col.Count(x => x.Name.StartsWith("mal")));
                Assert.AreEqual(1, col.Count(x => x.Name.Equals("Mauricio")));

                // > >= < <=
                Assert.AreEqual(1, col.Count(x => x.Id > 3));
                Assert.AreEqual(1, col.Count(x => x.Id >= 4));
                Assert.AreEqual(1, col.Count(x => x.Id < 2));
                Assert.AreEqual(1, col.Count(x => x.Id <= 1));

                // and/or
                Assert.AreEqual(1, col.Count(x => x.Id > 0 && x.Name == "MAURICIO"));
                Assert.AreEqual(2, col.Count(x => x.Name == "malafaia" || x.Name == "MAURICIO"));
            }
        }
    }
}
