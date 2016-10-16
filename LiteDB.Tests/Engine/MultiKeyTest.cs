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
                { "int", new BsonArray { 1, 2, 3 } },
                { "mix", new BsonArray { "a", 2, false } }
            };

            var i = string.Join(",", doc.GetValues("int"));
            var m = string.Join(",", doc.GetValues("mix"));
        }

        [TestMethod]
        public void MultiKey_Insert_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                db.Insert("col", GetDocs(1, 1, 1, 2, 3));
                db.Insert("col", GetDocs(2, 1, 2, 2, 4));
                db.Insert("col", GetDocs(3, 1, 3));

                // create index afer documents are in collection
                db.EnsureIndex("col", "list");
                db.EnsureIndex("col", "rnd");

                db.Update("col", GetDocs(2, 1, 9, 9));

                // try find
                var r = string.Join(",", db.Find("col", Query.EQ("list", 2)).Select(x => x["_id"].ToString()));

                Assert.AreEqual("1", r);


            }
        }

        private IEnumerable<BsonDocument> GetDocs(int start, int count, params int[] args)
        {
            var rnd = new Random();

            for(var i = start; i < start + count; i++)
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