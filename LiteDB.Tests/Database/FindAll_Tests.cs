using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests.Database
{
    #region Model

    public class Person
    {
        public int Id { get; set; }
        public string Fullname { get; set; }
    }

    #endregion

    [TestClass]
    public class FindAll_Tests
    {
        [TestMethod]
        public void FindAll()
        {
            using (var f = new TempFile())
            {
                using (var db = new LiteDatabase(f.Filename))
                {
                    var col = db.GetCollection<Person>("Person");

                    col.Insert(new Person { Fullname = "John" });
                    col.Insert(new Person { Fullname = "Doe" });
                    col.Insert(new Person { Fullname = "Joana" });
                    col.Insert(new Person { Fullname = "Marcus" });
                }
                // close datafile

                using (var db = new LiteDatabase(f.Filename))
                {
                    var p = db.GetCollection<Person>("Person").Find(Query.All("Fullname", Query.Ascending));

                    Assert.AreEqual(4, p.Count());
                }
            }

        }
    }
}