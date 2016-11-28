using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    [TestClass]
    public class DeleteByNameTest
    {
        [TestMethod]
        public void DeleteByName_Test()
        {
            using (var f = new TempFile())
            using (var db = new LiteDatabase(f.Filename))
            {
                var col = db.GetCollection<Person>("Person");

                col.Insert(new Tests.Person { Fullname = "John" });
                col.Insert(new Tests.Person { Fullname = "Doe" });
                col.Insert(new Tests.Person { Fullname = "Joana" });
                col.Insert(new Tests.Person { Fullname = "Marcus" });

                // lets auto-create index in FullName and delete from a non-pk node
                var del = col.Delete(x => x.Fullname.StartsWith("J"));

                Assert.AreEqual(2, del);
            }
        }
    }
}