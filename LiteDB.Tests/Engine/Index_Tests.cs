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
            using (var db = new LiteDatabase(new MemoryStream()))
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
    }
}