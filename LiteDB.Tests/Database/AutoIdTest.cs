using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    public class EntityInt
    {
        public int Id { get; set; }
    }

    public class EntityLong
    {
        public long Id { get; set; }
    }

    public class EntityGuid
    {
        public Guid Id { get; set; }
    }

    public class EntityOid
    {
        public ObjectId Id { get; set; }
    }

    public class EntityString
    {
        public string Id { get; set; }
    }

    [TestClass]
    public class AutoIdTest
    {
        [TestMethod]
        public void AutoId_Test()
        {
            var mapper = new BsonMapper();

            mapper.RegisterAutoId(
                (s) => s == null,
                (db, col) => "doc-" + db.Count(col, null)
            );

            // register for "long" - use an collection to manage 
            mapper.RegisterAutoId<long>(
                v => v == 0,
                (db, col) =>
                {
                    var seq = "_sequence";
                    db.BeginTrans();
                    var next = 0L;
                    try
                    {
                        var current = db.Find(seq, Query.EQ("_id", col)).FirstOrDefault();

                        if (current == null)
                        {
                            db.Insert(seq, new BsonDocument { { "_id", col }, { "value", 1L } });
                            next = 1;
                        }
                        else
                        {
                            next = current["value"] = current["value"].AsInt64 + 1;
                            db.Update(seq, current);
                        }

                        db.Commit();
                    }
                    catch (Exception)
                    {
                        db.Rollback();
                        throw;
                    }

                    return next;
                }
            );

            using (var file = new TempFile())
            using (var db = new LiteDatabase(file.Filename, mapper))
            {
                var cs_int = db.GetCollection<EntityInt>("int");
                var cs_long = db.GetCollection<EntityLong>("long");
                var cs_guid = db.GetCollection<EntityGuid>("guid");
                var cs_oid = db.GetCollection<EntityOid>("oid");
                var cs_str = db.GetCollection<EntityString>("str");

                // int32
                var cint_1 = new EntityInt { };
                var cint_2 = new EntityInt { };
                var cint_5 = new EntityInt { Id = 5 }; // set Id, do not generate (jump 3 and 4)!
                var cint_6 = new EntityInt { Id = 0 }; // for int, 0 is empty

                // long
                var clong_1 = new EntityLong { };
                var clong_2 = new EntityLong { };
                var clong_3 = new EntityLong { };
                var clong_4 = new EntityLong { };

                // guid
                var guid = Guid.NewGuid();

                var cguid_1 = new EntityGuid { Id = guid };
                var cguid_2 = new EntityGuid { };

                // oid
                var oid = ObjectId.NewObjectId();

                var coid_1 = new EntityOid { };
                var coid_2 = new EntityOid { Id = oid };

                // string - there is no AutoId for string
                var cstr_1 = new EntityString { };
                var cstr_2 = new EntityString { Id = "mydoc2" };

                cs_int.Insert(cint_1);
                cs_int.Insert(cint_2);
                cs_int.Insert(cint_5);
                cs_int.Insert(cint_6);

                cs_long.Insert(clong_1);
                cs_long.Insert(clong_2);
                // delete 1 and 2 and will not re-used
                cs_long.Delete(Query.In("_id", 1, 2));
                cs_long.Insert(clong_3);
                cs_long.Insert(clong_4);

                cs_guid.Insert(cguid_1);
                cs_guid.Insert(cguid_2);

                cs_oid.Insert(coid_1);
                cs_oid.Insert(coid_2);

                cs_str.Insert(cstr_1);

                // test for int
                Assert.AreEqual(1, cint_1.Id);
                Assert.AreEqual(2, cint_2.Id);
                Assert.AreEqual(5, cint_5.Id);
                Assert.AreEqual(6, cint_6.Id);

                // test for long
                Assert.AreEqual(3, clong_3.Id);
                Assert.AreEqual(4, clong_4.Id);

                // test for guid
                Assert.AreEqual(guid, cguid_1.Id);
                Assert.AreNotEqual(Guid.Empty, cguid_2.Id);
                Assert.AreNotEqual(cguid_2.Id, cguid_1.Id);

                // test for oid
                Assert.AreNotEqual(ObjectId.Empty, coid_1.Id);
                Assert.AreEqual(oid, coid_2.Id);

                // test for string
                Assert.AreEqual("doc-0", cstr_1.Id);
                Assert.AreEqual("mydoc2", cstr_2.Id);
            }
        }
    }
}