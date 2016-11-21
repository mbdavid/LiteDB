using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    public class Person
    {
        public int Id { get; set; }
        public string Fullname { get; set; }
    }

    [TestClass]
    public class FindAllTest
    {
        [TestMethod]
        public void FindAll_Test()
        {
            using (var f = new TempFile())
            {
                using (var db = new LiteDatabase(f.Filename))
                {
                    var col = db.GetCollection<Person>("Person");

                    col.Insert(new Tests.Person { Fullname = "John" });
                    col.Insert(new Tests.Person { Fullname = "Doe" });
                    col.Insert(new Tests.Person { Fullname = "Joana" });
                    col.Insert(new Tests.Person { Fullname = "Marcus" });
                }
                // close datafile

                using (var db = new LiteDatabase(f.Filename))
                {
                    var p = db.GetCollection<Person>("Person").Find(Query.All("Name", Query.Ascending));

                    Assert.AreEqual(4, p.Count());
                }
            }

        }
    }
}