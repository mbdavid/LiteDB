using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTest
{
    public class DataItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [TestClass]
    public class FindIndexTest
    {
        [TestMethod]
        public void Include_Test()
        {
            using (var db = new LiteDatabase(DB.Path()))
            {
                var col = db.GetCollection<DataItem>("col");

                col.Insert(new DataItem { Name = "Mau" });
                col.Insert(new DataItem { Name = "Marlon" });
                col.Insert(new DataItem { Name = "Mas" });
                col.Insert(new DataItem { Name = "Maur" });
                col.Insert(new DataItem { Name = "Mac" });

                col.EnsureIndex(x => x.Name, new IndexOptions { IgnoreCase = false });

                var upper3 = col.FindIndex(x => x.Id >= 3)
                    .Select(x => x.AsString)
                    .ToArray();

                Assert.AreEqual("345", string.Join("", upper3));

                var mau = col.FindIndex(x => x.Name.StartsWith("Mau"))
                    .Select(x => x.AsString)
                    .ToArray();

                Assert.AreEqual("Mau,Maur", string.Join(",", mau));

            }
        }
    }
}
