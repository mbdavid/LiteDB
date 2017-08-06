//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System;
//using System.IO;
//using System.Linq;

//namespace LiteDB.Tests.Database
//{
//    #region Model

//    public class EntityInt
//    {
//        public int Id { get; set; }
//    }

//    public class EntityLong
//    {
//        public long Id { get; set; }
//    }

//    public class EntityGuid
//    {
//        public Guid Id { get; set; }
//    }

//    public class EntityOid
//    {
//        public ObjectId Id { get; set; }
//    }

//    public class EntityString
//    {
//        public string Id { get; set; }
//    }

//    #endregion

//    [TestClass]
//    public class AutoId_Tests
//    {
//        [TestMethod, TestCategory("Database")]
//        public void AutoId_Strong_Typed_Test()
//        {
//            var mapper = new BsonMapper();

//            using (var file = new TempFile())
//            using (var db = new LiteDatabase(file.Filename, mapper))
//            {
//                var cs_int = db.GetCollection<EntityInt>("int");
//                var cs_long = db.GetCollection<EntityLong>("long");
//                var cs_guid = db.GetCollection<EntityGuid>("guid");
//                var cs_oid = db.GetCollection<EntityOid>("oid");
//                var cs_str = db.GetCollection<EntityString>("str");

//                // int32
//                var cint_1 = new EntityInt();
//                var cint_2 = new EntityInt();
//                var cint_3 = new EntityInt();
//                var cint_4 = new EntityInt();

//                // long
//                var clong_1 = new EntityLong();
//                var clong_2 = new EntityLong();
//                var clong_3 = new EntityLong();
//                var clong_4 = new EntityLong();

//                // guid
//                var cguid_1 = new EntityGuid();
//                var cguid_2 = new EntityGuid();

//                // oid
//                var coid_1 = new EntityOid();
//                var coid_2 = new EntityOid();

//                // string - there is no AutoId for string
//                var cstr_1 = new EntityString();
//                var cstr_2 = new EntityString();

//                cs_int.Insert(new [] { cint_1, cint_2, cint_3, cint_4 });

//                cs_long.Insert(new [] { clong_1, clong_2 });
//                // delete 1 and 2 and will not re-used
//                cs_long.Delete(Query.In("_id", 1, 2));
//                cs_long.Insert(clong_3);
//                cs_long.Insert(clong_4);

//                cs_guid.Insert(new [] { cguid_1, cguid_2 });
//                cs_oid.Insert(new [] { coid_1, coid_2 });
//                cs_str.Insert(new [] { cstr_1, cstr_2 });

//                // test for int
//                Assert.AreEqual(1, cint_1.Id);
//                Assert.AreEqual(2, cint_2.Id);
//                Assert.AreEqual(3, cint_3.Id);
//                Assert.AreEqual(4, cint_4.Id);

//                // test for long
//                Assert.AreEqual(3, clong_3.Id);
//                Assert.AreEqual(4, clong_4.Id);

//                // test for guid
//                Assert.AreNotEqual(Guid.Empty, cguid_1.Id);
//                Assert.AreNotEqual(Guid.Empty, cguid_2.Id);
//                Assert.AreNotEqual(cguid_2.Id, cguid_1.Id);

//                // test for oid
//                Assert.AreNotEqual(ObjectId.Empty, coid_1.Id);
//                Assert.AreNotEqual(ObjectId.Empty, coid_2.Id);

//                // test for string
//                Assert.AreEqual("1", cstr_1.Id);
//                Assert.AreEqual("2", cstr_2.Id);
//            }
//        }
//    }
//}