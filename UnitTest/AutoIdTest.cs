using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTest
{
    public class CustomerWithInt
    {
        [BsonId(true)]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CustomerWithGuid
    {
        [BsonId(true)]
        public Guid Id { get; set; }
        public string Name { get; set; }
    }


    [TestClass]
    public class AutoIdTest
    {
        [TestMethod]
        public void AutoId_Test()
        {
            using (var db = new LiteDatabase(DB.Path()))
            {
                var cs_int = db.GetCollection<CustomerWithInt>("CustomerWithInt");
                var cs_guid = db.GetCollection<CustomerWithGuid>("CustomerWithGuid");

                var guid = Guid.NewGuid();

                var cint_1 = new CustomerWithInt { Name = "Using Int 1" };
                var cint_2 = new CustomerWithInt { Name = "Using Int 2" };
                var cint_5 = new CustomerWithInt { Id = 5, Name = "Using Int 5" }; // set Id, do not generate!
                var cint_6 = new CustomerWithInt { Id = 0, Name = "Using Int 6" }; // for int, 0 is empty

                var cguid_1 = new CustomerWithGuid { Id = guid, Name = "Using Guid" };
                var cguid_2 = new CustomerWithGuid { Name = "Using Guid" };

                cs_int.Insert(cint_1);
                cs_int.Insert(cint_2);
                cs_int.Insert(cint_5);
                cs_int.Insert(cint_6);

                cs_guid.Insert(cguid_1);
                cs_guid.Insert(cguid_2);


                Assert.AreEqual(cint_1.Id, 1);
                Assert.AreEqual(cint_2.Id, 2);
                Assert.AreEqual(cint_5.Id, 5);
                Assert.AreEqual(cint_6.Id, 6);

                Assert.AreEqual(cguid_1.Id, guid);
                Assert.AreNotEqual(cguid_2.Id, Guid.Empty);
                Assert.AreNotEqual(cguid_1.Id, cguid_2.Id);

            }
        }
    }
}
