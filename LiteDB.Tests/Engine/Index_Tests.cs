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
            using (var db = new LiteEngine())
            {
                db.Insert("users", new BsonDocument { ["name"] = new BsonDocument { ["first"] = "John", ["last"] = "Doe" } });
                db.Insert("users", new BsonDocument { ["name"] = new BsonDocument { ["first"] = "Marco", ["last"] = "Pollo" } });

                // no index name defined
                db.EnsureIndex("users", "name.last");
                db.EnsureIndex("users", "$.name.first", true);

                // default name: remove all non-[a-z] chars
                Assert.IsNotNull(db.Query("$indexes").Where("collection = 'users' AND name = 'namelast'").ExecuteScalar());
                Assert.IsNotNull(db.Query("$indexes").Where("collection = 'users' AND name = 'namefirst'").ExecuteScalar());
            }
        }
    }
}