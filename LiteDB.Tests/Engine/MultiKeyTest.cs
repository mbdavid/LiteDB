using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests
{
    [TestClass]
    public class MultiKeyTest
    {
        [TestMethod]
        public void MultiKey_GetValues_Test()
        {
            var doc = new BsonDocument
            {
                { "int", new BsonArray { 1, 2, 3, new BsonArray { 4, 5, new BsonArray { 6, 5, 4 } } } },
                { "addr", new BsonArray {
                    new BsonDocument { { "street", "av. protasio" } },
                    new BsonDocument { { "street", "av. ipiranga" } }
                } },
                { "list", new BsonArray {
                    new BsonDocument { {
                        "phones", new BsonArray {
                            new BsonDocument { { "number", "123" } },
                            new BsonDocument { { "number", "456" } }
                        } } },
                    new BsonDocument { {
                        "phones", new BsonArray {
                            new BsonDocument { { "number", new BsonArray { "789", "***" } } },
                        } } }
                } },
                { "say", new BsonDocument { { "my", new BsonDocument { { "name", "Heisenberg" } } } } }
            };

            var i = string.Join(",", doc.GetValues("int").Select(x => x.AsString));
            var i2 = string.Join(",", doc.GetValues("int", true).Select(x => x.AsString));
            var s = string.Join(",", doc.GetValues("addr.street").Select(x => x.AsString));
            var p = string.Join(",", doc.GetValues("list.phones.number").Select(x => x.AsString));
            var h = string.Join(",", doc.GetValues("say.my.name").Select(x => x.AsString));
            var nf = string.Join(",", doc.GetValues("say.notfound").Select(x => x.AsString));

            Assert.AreEqual("1,2,3,4,5,6,5,4", i);
            Assert.AreEqual("1,2,3,4,5,6", i2);
            Assert.AreEqual("av. protasio,av. ipiranga", s);
            Assert.AreEqual("123,456,789,***", p);
            Assert.AreEqual("Heisenberg", h);
            Assert.AreEqual("", nf);
        }

        [TestMethod]
        public void MultiKey_InsertUpdate_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                db.Insert("col", GetDocs(1, 1, 1, 2, 3));
                db.Insert("col", GetDocs(2, 2, 2, 2, 4));
                db.Insert("col", GetDocs(3, 3, 3));

                // create index afer documents are in collection
                db.EnsureIndex("col", "list");
                db.EnsureIndex("col", "rnd");

                db.Update("col", GetDocs(2, 2, 9, 9));

                // try find
                var r = string.Join(",", db.Find("col", Query.EQ("list", 2)).Select(x => x["_id"].ToString()));

                Assert.AreEqual("1", r);
                Assert.AreEqual(3, db.Count("col", null));
                Assert.AreEqual(3, db.Count("col", Query.All()));

                // 5 keys = [1, 2, 3],[3],[9]
                var l = string.Join(",", db.FindIndex("col", Query.All("list")));

                Assert.AreEqual("1,2,3,3,9", l);

                // count should be count only documents - not index nodes
                Assert.AreEqual(3, db.Count("col", Query.All("list")));

            }
        }

        public void Multikey_Count_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Disk(), cacheSize: 10))
            {
                // create index before
                db.EnsureIndex("col", "list");

                db.Insert("col", GetDocs(1, 1000, 1, 2, 3));
                db.Insert("col", GetDocs(1001, 2000, 2, 3));
                db.Insert("col", GetDocs(2001, 2500, 4));

                Assert.AreEqual(1000, db.Count("col", Query.All("list", 1)));
                Assert.AreEqual(2000, db.Count("col", Query.All("list", 2)));
                Assert.AreEqual(2000, db.Count("col", Query.All("list", 3)));
                Assert.AreEqual(500, db.Count("col", Query.All("list", 4)));

                // drop index
                db.DropIndex("col", "list");

                // re-create index
                db.EnsureIndex("col", "list");

                // count again
                Assert.AreEqual(1000, db.Count("col", Query.All("list", 1)));
                Assert.AreEqual(2000, db.Count("col", Query.All("list", 2)));
                Assert.AreEqual(2000, db.Count("col", Query.All("list", 3)));
                Assert.AreEqual(500, db.Count("col", Query.All("list", 4)));
            }
        }

        private IEnumerable<BsonDocument> GetDocs(int start, int end, params int[] args)
        {
            var rnd = new Random();

            for(var i = start; i <= end; i++)
            {
                yield return new BsonDocument
                {
                    { "_id", i },
                    { "list", new BsonArray(args.Select(x => new BsonValue(x))) },
                    { "rnd", rnd.Next(0, 100) }
                };
            }
        }
    }
}