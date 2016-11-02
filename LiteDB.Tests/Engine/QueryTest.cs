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
    public class QueryTest
    {
        [TestMethod]
        public void Query_Test()
        {
            using (var file = new TempFile())
            using (var db = new LiteEngine(file.Filename))
            {
                db.Insert("col", new BsonDocument[]
                {
                    new BsonDocument { { "_id", 1 }, { "name", "e" } },
                    new BsonDocument { { "_id", 2 }, { "name", "d" } },
                    new BsonDocument { { "_id", 3 }, { "name", "c" } },
                    new BsonDocument { { "_id", 4 }, { "name", "b" } },
                    new BsonDocument { { "_id", 5 }, { "name", "a" } }
                });

                db.EnsureIndex("col", "name");

                Func<Query, string> result = (q) => string.Join(",", db.FindIndex("col", q).Select(x => x.ToString()));

                Assert.AreEqual("1", result(Query.EQ("_id", 1)));
                Assert.AreEqual("4,5", result(Query.GTE("_id", 4)));
                Assert.AreEqual("1", result(Query.LT("_id", 2)));
                Assert.AreEqual("a,b,d,e", result(Query.Not("name", "c")));
                Assert.AreEqual("2,4", result(Query.Where("_id", (v) => v.AsInt32 % 2 == 0)));
            }
        }
    }
}