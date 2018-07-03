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

namespace LiteDB.Tests.Query
{
    [TestClass]
    public class GroupBy_Tests
    {
        private LiteEngine db;
        private BsonDocument[] person;

        [TestInitialize]
        public void Init()
        {
            db = new LiteEngine();
            person = DataGen.Person(1, 1000).ToArray();

            db.Insert("person", person);
            db.EnsureIndex("col", "age");
        }

        [TestCleanup]
        public void CleanUp()
        {
            db.Dispose();
        }

        [TestMethod]
        public void Query_GroupBy_State_With_Count()
        {
            var r0 = person
                .GroupBy(x => x["state"])
                .Select(x => new BsonDocument { ["state"] = x.Key, ["count"] = x.Count() })
                .ToArray();

            var r1 = db.Query("person")
                .GroupBy("state")
                .Select("{ state, count: COUNT($) }")
                .ToArray();

            // check for all states counts
            Util.Compare(r0, r1, true);
        }

        [TestMethod]
        public void Query_GroupBy_State_With_Sum()
        {
            var r0 = person
                .GroupBy(x => x["state"])
                .Select(x => new BsonDocument { ["state"] = x.Key, ["sum"] = x.Sum(q => q["age"].AsInt32) })
                .ToArray();

            var r1 = db.Query("person")
                .GroupBy("state")
                .Select("{ state, sum: SUM(age) }")
                .ToArray();

            Util.Compare(r0, r1, true);
        }

        [TestMethod]
        public void Query_GroupBy_State_With_Filter_And_OrderBy()
        {
            var r0 = person
                .Where(x => x["age"] > 35)
                .GroupBy(x => x["state"])
                .Select(x => new BsonDocument { ["state"] = x.Key, ["count"] = x.Count() })
                .OrderBy(x => x["state"].AsString)
                .ToArray();

            var r1 = db.Query("person")
                .Where("age > 35")
                .GroupBy("state")
                .Select("{ state, count: COUNT($) }")
                .OrderBy("state")
                .ToArray();

            Util.Compare(r0, r1, true);
        }

        [TestMethod]
        public void Query_GroupBy_Func()
        {
            var r0 = person
                .GroupBy(x => x["date"].AsDateTime.Year)
                .Select(x => new BsonDocument { ["year"] = x.Key, ["count"] = x.Count() })
                .ToArray();

            var r1 = db.Query("person")
                .GroupBy("YEAR(date)")
                .Select("{ year: YEAR(date), count: COUNT($) }")
                .ToArray();

            Util.Compare(r0, r1, true);
        }

        [TestMethod]
        public void Query_GroupBy_With_Array_Aggregation()
        {
            var r = db.Query("person")
                .GroupBy("SUBSTRING(email, INDEXOF(email, '@') + 1)")
                .Select(@"{ 
                               domain: SUBSTRING(email, INDEXOF(email, '@') + 1), 
                               users: [{
                                   user: LOWER(SUBSTRING(email, 0, INDEXOF(email, '@'))), 
                                   name, 
                                   age
                               }]
                           }")
                .OrderBy("LENGTH(users)", -1)
                .Limit(10)
                .ToArray();

            for(var i = 0; i < r.Length; i++)
            {
                var users0 = usersDomain(r[i]["domain"]);
                var users1 = r[i]["users"].AsArray;

                if (users0.CompareTo(users1) != 0)
                {
                    Assert.Fail($"Result are not same: {users0} and {users1}");
                }
            }

            // return all users from a single email domain
            BsonArray usersDomain(string domain) {
                return person
                    .Where(x => x["email"].AsString.Substring(x["email"].AsString.IndexOf('@') + 1) == domain)
                    .Select(x => new BsonDocument {
                        ["user"] = x["email"].AsString.ToLower().Substring(0, x["email"].AsString.IndexOf('@')),
                        ["name"] = x["name"],
                        ["age"] = x["age"]
                    })
                    .ToBsonArray();
            };
        }
    }
}