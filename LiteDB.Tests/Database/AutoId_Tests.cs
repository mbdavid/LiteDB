using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests.Database
{
    #region Model

    public class EntityInt
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class EntityLong
    {
        public long Id { get; set; }
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

    #endregion

    [TestClass]
    public class AutoId_Tests
    {
        [TestMethod]
        public void AutoId_Strong_Typed()
        {
            var mapper = new BsonMapper();

            using (var db = new LiteDatabase(new MemoryStream(), mapper))
            {
                var cs_int = db.GetCollection<EntityInt>("int");
                var cs_long = db.GetCollection<EntityLong>("long");
                var cs_guid = db.GetCollection<EntityGuid>("guid");
                var cs_oid = db.GetCollection<EntityOid>("oid");
                var cs_str = db.GetCollection<EntityString>("str");

                // int32
                var cint_1 = new EntityInt() { Name = "R1" };
                var cint_2 = new EntityInt() { Name = "R2" };
                var cint_3 = new EntityInt() { Name = "R3" };
                var cint_4 = new EntityInt() { Name = "R4" };

                // long
                var clong_1 = new EntityLong() { Name = "R1" };
                var clong_2 = new EntityLong() { Name = "R2" };
                var clong_3 = new EntityLong() { Name = "R3" };
                var clong_4 = new EntityLong() { Name = "R4" };

                // guid
                var cguid_1 = new EntityGuid() { Name = "R1" };
                var cguid_2 = new EntityGuid() { Name = "R2" };
                var cguid_3 = new EntityGuid() { Name = "R3" };
                var cguid_4 = new EntityGuid() { Name = "R4" };

                // oid
                var coid_1 = new EntityOid() { Name = "R1" };
                var coid_2 = new EntityOid() { Name = "R2" };
                var coid_3 = new EntityOid() { Name = "R3" };
                var coid_4 = new EntityOid() { Name = "R4" };

                // string - there is no AutoId for string
                var cstr_1 = new EntityString() { Id = "a", Name = "R1" };
                var cstr_2 = new EntityString() { Id = "b", Name = "R2" };
                var cstr_3 = new EntityString() { Id = "c", Name = "R3" };
                var cstr_4 = new EntityString() { Id = "d", Name = "R4" };

                // insert first 3 documents
                cs_int.Insert(new[] { cint_1, cint_2, cint_3 });
                cs_long.Insert(new[] { clong_1, clong_2, clong_3 });
                cs_guid.Insert(new[] { cguid_1, cguid_2, cguid_3 });
                cs_oid.Insert(new[] { coid_1, coid_2, coid_3 });
                cs_str.Insert(new[] { cstr_1, cstr_2, cstr_3 });

                // change document 2
                cint_2.Name = "Changed 2";
                clong_2.Name = "Changed 2";
                cguid_2.Name = "Changed 2";
                coid_2.Name = "Changed 2";
                cstr_2.Name = "Changed 2";

                // update document 2
                var nu_int = cs_int.Update(cint_2);
                var nu_long = cs_long.Update(clong_2);
                var nu_guid = cs_guid.Update(cguid_2);
                var nu_oid = cs_oid.Update(coid_2);
                var nu_str = cs_str.Update(cstr_2);

                Assert.IsTrue(nu_int);
                Assert.IsTrue(nu_long);
                Assert.IsTrue(nu_guid);
                Assert.IsTrue(nu_oid);
                Assert.IsTrue(nu_str);

                // change document 3
                cint_3.Name = "Changed 3";
                clong_3.Name = "Changed 3";
                cguid_3.Name = "Changed 3";
                coid_3.Name = "Changed 3";
                cstr_3.Name = "Changed 3";

                // upsert (update) document 3
                var fu_int = cs_int.Upsert(cint_3);
                var fu_long = cs_long.Upsert(clong_3);
                var fu_guid = cs_guid.Upsert(cguid_3);
                var fu_oid = cs_oid.Upsert(coid_3);
                var fu_str = cs_str.Upsert(cstr_3);

                Assert.IsFalse(fu_int);
                Assert.IsFalse(fu_long);
                Assert.IsFalse(fu_guid);
                Assert.IsFalse(fu_oid);
                Assert.IsFalse(fu_str);

                // test if was changed
                Assert.AreEqual(cint_3.Name, cs_int.FindOne(x => x.Id == cint_3.Id).Name);
                Assert.AreEqual(clong_3.Name, cs_long.FindOne(x => x.Id == clong_3.Id).Name);
                Assert.AreEqual(cguid_3.Name, cs_guid.FindOne(x => x.Id == cguid_3.Id).Name);
                Assert.AreEqual(coid_3.Name, cs_oid.FindOne(x => x.Id == coid_3.Id).Name);
                Assert.AreEqual(cstr_3.Name, cs_str.FindOne(x => x.Id == cstr_3.Id).Name);

                // upsert (insert) document 4
                var tu_int = cs_int.Upsert(cint_4);
                var tu_long = cs_long.Upsert(clong_4);
                var tu_guid = cs_guid.Upsert(cguid_4);
                var tu_oid = cs_oid.Upsert(coid_4);
                var tu_str = cs_str.Upsert(cstr_4);

                Assert.IsTrue(tu_int);
                Assert.IsTrue(tu_long);
                Assert.IsTrue(tu_guid);
                Assert.IsTrue(tu_oid);
                Assert.IsTrue(tu_str);

                // test if was included
                Assert.AreEqual(cint_4.Name, cs_int.FindOne(x => x.Id == cint_4.Id).Name);
                Assert.AreEqual(clong_4.Name, cs_long.FindOne(x => x.Id == clong_4.Id).Name);
                Assert.AreEqual(cguid_4.Name, cs_guid.FindOne(x => x.Id == cguid_4.Id).Name);
                Assert.AreEqual(coid_4.Name, cs_oid.FindOne(x => x.Id == coid_4.Id).Name);
                Assert.AreEqual(cstr_4.Name, cs_str.FindOne(x => x.Id == cstr_4.Id).Name);

                // count must be 4
                Assert.AreEqual(4, cs_int.Count(Query.All()));
                Assert.AreEqual(4, cs_long.Count(Query.All()));
                Assert.AreEqual(4, cs_guid.Count(Query.All()));
                Assert.AreEqual(4, cs_oid.Count(Query.All()));
                Assert.AreEqual(4, cs_str.Count(Query.All()));

                // for Int32 (or Int64) - add "bouble" on sequence
                var cint_10 = new EntityInt { Id = 10, Name = "R10" };
                var cint_11 = new EntityInt { Name = "R11" };
                var cint_7 = new EntityInt { Id = 7, Name = "R7" };
                var cint_12 = new EntityInt { Name = "R12" };

                cs_int.Insert(cint_10); // "loose" sequente between 5-9
                cs_int.Insert(cint_11); // insert as 11
                cs_int.Insert(cint_7); // insert as 7
                cs_int.Insert(cint_12); // insert as 12

                Assert.AreEqual(10, cint_10.Id);
                Assert.AreEqual(11, cint_11.Id);
                Assert.AreEqual(7, cint_7.Id);
                Assert.AreEqual(12, cint_12.Id);
            }
        }

        [TestMethod]
        public void AutoId_BsonDocument()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var col = db.GetCollection("Writers");
                col.Insert(new BsonDocument { ["Name"] = "Mark Twain" });
                col.Insert(new BsonDocument { ["Name"] = "Jack London", ["_id"] = 1 });

                // create an index in name field
                col.EnsureIndex("LowerName", "LOWER($.Name)");

                var mark = col.FindOne(Query.EQ("LowerName", "mark twain"));
                var jack = col.FindOne(Query.EQ("LowerName", "jack london"));

                // checks if auto-id is a ObjectId
                Assert.IsTrue(mark["_id"].IsObjectId);
                Assert.IsTrue(jack["_id"].IsInt32); // jack do not use AutoId (fixed in int32)
            }
        }

        [TestMethod]
        public void AutoId_No_Duplicate_After_Delete()
        {
            // using strong type
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var col = db.GetCollection<EntityInt>("col1");

                var one = new EntityInt { Name = "One" };
                var two = new EntityInt { Name = "Two" };
                var three = new EntityInt { Name = "Three" };
                var four = new EntityInt { Name = "Four" };

                // insert
                col.Insert(one);
                col.Insert(two);

                Assert.AreEqual(1, one.Id);
                Assert.AreEqual(2, two.Id);

                // now delete first 2 rows
                col.Delete(one.Id);
                col.Delete(two.Id);

                // and insert new documents
                col.Insert(new EntityInt[] { three, four });

                Assert.AreEqual(3, three.Id);
                Assert.AreEqual(4, four.Id);
            }

            // using bsondocument/engine
            using (var db = new LiteEngine(new MemoryStream()))
            {
                var one = new BsonDocument { ["Name"] = "One" };
                var two = new BsonDocument { ["Name"] = "Two" };
                var three = new BsonDocument { ["Name"] = "Three" };
                var four = new BsonDocument { ["Name"] = "Four" };

                db.Insert("col", one, BsonType.Int32);
                db.Insert("col", two, BsonType.Int32);

                Assert.AreEqual(1, one["_id"].AsInt32);
                Assert.AreEqual(2, two["_id"].AsInt32);

                // now delete first 2 rows
                db.Delete("col", one["_id"].AsInt32);
                db.Delete("col", two["_id"].AsInt32);

                // and insert new documents
                db.Insert("col", new BsonDocument[] { three, four }, BsonType.Int32);

                Assert.AreEqual(3, three["_id"].AsInt32);
                Assert.AreEqual(4, four["_id"].AsInt32);
            }
        }
    }
}