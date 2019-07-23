using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using LiteDB.Engine;
using System.Threading;
using System.Linq.Expressions;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Index_Tests
    {
        [TestMethod]
        public void Index_With_No_Name()
        {
            using (var db = new LiteDatabase("filename=:memory:"))
            {
                var users = db.GetCollection("users");
                var indexes = db.GetCollection("$indexes");

                users.Insert(new BsonDocument { ["name"] = new BsonDocument { ["first"] = "John", ["last"] = "Doe" } });
                users.Insert(new BsonDocument { ["name"] = new BsonDocument { ["first"] = "Marco", ["last"] = "Pollo" } });

                // no index name defined
                users.EnsureIndex("name.last");
                users.EnsureIndex("$.name.first", true);

                // default name: remove all non-[a-z] chars
                Assert.IsNotNull(indexes.FindOne("collection = 'users' AND name = 'namelast'"));
                Assert.IsNotNull(indexes.FindOne("collection = 'users' AND name = 'namefirst'"));
            }
        }

        [TestMethod]
        public void Index_Order()
        {
            using (var db = new LiteDatabase("filename=:memory:"))
            {
                var col = db.GetCollection("col");
                var indexes = db.GetCollection("$indexes");

                col.Insert(new BsonDocument { { "text", "D" } });
                col.Insert(new BsonDocument { { "text", "A" } });
                col.Insert(new BsonDocument { { "text", "E" } });
                col.Insert(new BsonDocument { { "text", "C" } });
                col.Insert(new BsonDocument { { "text", "B" } });

                col.EnsureIndex("text");

                var asc = string.Join("", col.Query()
                    .OrderBy("text")
                    .Select("text")
                    .ToDocuments()
                    .Select(x => x["text"].AsString));

                var desc = string.Join("", col.Query()
                    .OrderByDescending("text")
                    .Select("text")
                    .ToDocuments()
                    .Select(x => x["text"].AsString));

                Assert.AreEqual("ABCDE", asc);
                Assert.AreEqual("EDCBA", desc);

                var rr = indexes.Query().ToList();

                Assert.AreEqual(1, indexes.Count("name = 'text'"));
            }
        }

        [TestMethod]
        public void Index_With_Like()
        {
            using (var db = new LiteDatabase("filename=:memory:"))
            {
                var col = db.GetCollection("names", BsonAutoId.Int32);

                col.Insert(new[] {
                    new BsonDocument { ["name"] = "marcelo" },
                    new BsonDocument { ["name"] = "mauricio" },
                    new BsonDocument { ["name"] = "Mauricio" },
                    new BsonDocument { ["name"] = "MAUricio" },
                    new BsonDocument { ["name"] = "MAURICIO" },
                    new BsonDocument { ["name"] = "mauRO" },
                    new BsonDocument { ["name"] = "ANA" }
                });

                col.EnsureIndex("idx_name", "name", false);

                var all = db.Execute("SELECT name FROM names").ToArray();

                // LIKE are case insensitive

                var r0 = db.Execute("SELECT name FROM names WHERE name LIKE 'Mau%'").ToArray();
                var r1 = db.Execute("SELECT name FROM names WHERE name LIKE 'MAU%'").ToArray();
                var r2 = db.Execute("SELECT name FROM names WHERE name LIKE 'mau%'").ToArray();

                Assert.AreEqual(5, r0.Length);
                Assert.AreEqual(5, r1.Length);
                Assert.AreEqual(5, r2.Length);

                // only `mauricio´
                var r3 = db.Execute("SELECT name FROM names WHERE name LIKE 'ma%ci%'").ToArray();
                var r4 = db.Execute("SELECT name FROM names WHERE name LIKE 'maUriCIO").ToArray();

                Assert.AreEqual(4, r3.Length);
                Assert.AreEqual(4, r4.Length);

                var r5 = db.Execute("SELECT name FROM names WHERE name LIKE 'marc_o").ToArray();

                Assert.AreEqual(0, r5.Length);

                // `marcelo`
                var r6 = db.Execute("SELECT name FROM names WHERE name LIKE 'marc__o").ToArray();

                Assert.AreEqual(1, r6.Length);

            }
        }
    }
}