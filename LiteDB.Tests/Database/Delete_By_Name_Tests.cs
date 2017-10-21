using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests.Database
{
    [TestClass]
    public class Delete_By_Name_Tests
    {
        [TestMethod]
        public void Delete_By_Name()
        {
            using (var f = new TempFile())
            using (var db = new LiteDatabase(f.Filename))
            {
                var col = db.GetCollection<Person>("Person");

                col.Insert(new Person { Fullname = "John" });
                col.Insert(new Person { Fullname = "Doe" });
                col.Insert(new Person { Fullname = "Joana" });
                col.Insert(new Person { Fullname = "Marcus" });

                // lets auto-create index in FullName and delete from a non-pk node
                var del = col.Delete(x => x.Fullname.StartsWith("J"));

                Assert.AreEqual(2, del);
            }
        }
    }
}