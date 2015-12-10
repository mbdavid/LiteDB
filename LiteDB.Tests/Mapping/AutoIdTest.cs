using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace LiteDB.Tests
{
    public class EntityInt
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class EntityGuid
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class EntityOid
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
    }

    public class EntityString
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    [TestClass]
    public class AutoIdTest
    {
        [TestMethod]
        public void AutoId_Test()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var cs_int = db.GetCollection<EntityInt>("int");
                var cs_guid = db.GetCollection<EntityGuid>("guid");
                var cs_oid = db.GetCollection<EntityOid>("oid");
                var cs_str = db.GetCollection<EntityString>("str");

                // int32
                var cint_1 = new EntityInt { Name = "Using Int 1" };
                var cint_2 = new EntityInt { Name = "Using Int 2" };
                var cint_5 = new EntityInt { Id = 5, Name = "Using Int 5" }; // set Id, do not generate (jump 3 and 4)!
                var cint_6 = new EntityInt { Id = 0, Name = "Using Int 6" }; // for int, 0 is empty

                // guid
                var guid = Guid.NewGuid();

                var cguid_1 = new EntityGuid { Id = guid, Name = "Using Guid" };
                var cguid_2 = new EntityGuid { Name = "Using Guid" };

                // oid
                var oid = ObjectId.NewObjectId();

                var coid_1 = new EntityOid { Name = "ObjectId-1" };
                var coid_2 = new EntityOid { Id = oid, Name = "ObjectId-2" };

                // string - there is no AutoId for string
                var cstr_1 = new EntityString { Name = "Object using String" };

                cs_int.Insert(cint_1);
                cs_int.Insert(cint_2);
                cs_int.Insert(cint_5);
                cs_int.Insert(cint_6);

                cs_guid.Insert(cguid_1);
                cs_guid.Insert(cguid_2);

                cs_oid.Insert(coid_1);
                cs_oid.Insert(coid_2);

                try
                {
                    cs_str.Insert(cstr_1);
                    Assert.Fail();
                }
                catch (LiteException)
                {
                    // must fail because EntityString class has a defined Id
                    //   but has no value and no auto_id funtion - issue #43
                }

                // test for int
                Assert.AreEqual(cint_1.Id, 1);
                Assert.AreEqual(cint_2.Id, 2);
                Assert.AreEqual(cint_5.Id, 5);
                Assert.AreEqual(cint_6.Id, 6);

                // test for guid
                Assert.AreEqual(cguid_1.Id, guid);
                Assert.AreNotEqual(cguid_2.Id, Guid.Empty);
                Assert.AreNotEqual(cguid_1.Id, cguid_2.Id);

                // test for oid
                Assert.AreNotEqual(coid_1, ObjectId.Empty);
                Assert.AreEqual(coid_2.Id, oid);
            }
        }
    }
}