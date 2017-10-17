using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Query_Tests
    {
        [TestMethod]
        public void Query_Using_Index_Search()
        {
            ExecuteQuery(true);
        }

        [TestMethod]
        public void Query_Using_Fullscan_Search()
        {
            ExecuteQuery(false);
        }

        public void ExecuteQuery(bool createIndex)
        {
            using (var db = new LiteEngine(new MemoryStream()))
            {
                db.Insert("col", new BsonDocument[]
                {
                    new BsonDocument { ["age"] = 1, ["name"] = "a" },
                    new BsonDocument { ["age"] = 2, ["name"] = "b" },
                    new BsonDocument { ["age"] = 3, ["name"] = "c" },
                    new BsonDocument { ["age"] = 4, ["name"] = "d" },
                    new BsonDocument { ["age"] = 5, ["name"] = "e" },
                    new BsonDocument { ["age"] = 6, ["name"] = "f" },
                    new BsonDocument { ["age"] = 7, ["name"] = "g" },
                    new BsonDocument { ["age"] = 8, ["name"] = "h" },
                    new BsonDocument { ["age"] = 9, ["name"] = "i" },
                    new BsonDocument { ["age"] = 9, ["name"] = "j" }
                });

                if (createIndex)
                {
                    db.EnsureIndex("col", "age");
                    db.EnsureIndex("col", "name");
                }

                Func<Query, string> result = (q) => string.Join(",", db.Find("col", q).Select(x => x["name"].AsString));

                Assert.AreEqual("a,b,c,d,e,f,g,h,i,j", result(Query.All()));

                Assert.AreEqual("a", result(Query.EQ("age", 1)));
                Assert.AreEqual("g", result(Query.EQ("age", 7)));

                Assert.AreEqual("h,i,j", result(Query.GT("age", 7)));
                Assert.AreEqual("g,h,i,j", result(Query.GTE("age", 7)));

                Assert.AreEqual("", result(Query.LT("age", 1)));
                Assert.AreEqual("a", result(Query.LTE("age", 1)));

                Assert.AreEqual("g,h,i,j", result(Query.Between("age", 7, 9)));

                Assert.AreEqual("a,b,c,d,e,f,g,h", result(Query.Not("age", 9)));
                Assert.AreEqual("a", result(Query.Not(Query.GTE("age", 2))));
                Assert.AreEqual("a,g,i,j", result(Query.In("age", 1, 7, 9)));
                Assert.AreEqual("a", result(Query.StartsWith("name", "a")));

                Assert.AreEqual("j", result(Query.And(Query.EQ("age", 9), Query.EQ("name", "j"))));

                Assert.AreEqual("j", result(Query.And(Query.GTE("age", 1), Query.And(Query.LTE("age", 9), Query.EQ("name", "j")))));

                Assert.AreEqual("j", result(Query.And(Query.GTE("age", 1), Query.LTE("age", 9), Query.EQ("name", "j"))));

                Assert.AreEqual("a,i,j", result(Query.Or(Query.EQ("age", 1), Query.EQ("age", 9))));

                Assert.AreEqual("b,d,f,h", result(Query.Where("age", (v) => v.AsInt32 % 2 == 0)));
            }
        }

        [TestMethod]
        public void Query_Using_First_Linq()
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

                var first = db.Find("col", Query.All()).First();

                Assert.AreEqual("e", first["name"].AsString);

            }
        }
    }
}