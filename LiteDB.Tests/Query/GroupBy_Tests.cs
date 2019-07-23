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
        private Person[] local;

        private LiteDatabase db;
        private LiteCollection<Person> collection;

        [TestInitialize]
        public void Init()
        {
            local = DataGen.Person(1, 1000).ToArray();

            db = new LiteDatabase(new MemoryStream());
            collection = db.GetCollection<Person>();

            collection.Insert(local);
            collection.EnsureIndex(x => x.Age);
        }

        [TestCleanup]
        public void CleanUp()
        {
            db.Dispose();
        }

        [TestMethod]
        public void Query_GroupBy_State_With_Count()
        {
            //** var r0 = local
            //**     .GroupBy(x => x.State)
            //**     .Select(x => new { State = x.Key, Count = x.Count() })
            //**     .OrderBy(x => x.State)
            //**     .ToArray();
            //** 
            //** var r1 = collection.Query()
            //**     .GroupBy(x => x.State)
            //**     .Select(x => new { x.State, Count = Sql.Count(x) })
            //**     .OrderBy(x => x.State)
            //**     .ToArray();
            //** 
            //** foreach (var r in r0.Zip(r1, (l, r) => new { left = l, right = r }))
            //** {
            //**     Assert.AreEqual(r.left.State, r.right.State);
            //**     Assert.AreEqual(r.left.Count, r.right.Count);
            //** }
        }

        [TestMethod]
        public void Query_GroupBy_State_With_Sum_Age()
        {
            //** var r0 = local
            //**     .GroupBy(x => x.State)
            //**     .Select(x => new { State = x.Key, Sum = x.Sum(q => q.Age) })
            //**     .OrderBy(x => x.State)
            //**     .ToArray();
            //** 
            //** var r1 = collection.Query()
            //**     .GroupBy(x => x.State)
            //**     .Select(x => new { x.State, Sum = Sql.Sum(x.Age) })
            //**     .OrderBy(x => x.State)
            //**     .ToArray();
            //** 
            //** foreach (var r in r0.Zip(r1, (l, r) => new { left = l, right = r }))
            //** {
            //**     Assert.AreEqual(r.left.State, r.right.State);
            //**     Assert.AreEqual(r.left.Sum, r.right.Sum);
            //** }
        }

        [TestMethod]
        public void Query_GroupBy_Func()
        {
            //** var r0 = local
            //**     .GroupBy(x => x.Date.Year)
            //**     .Select(x => new { Year = x.Key, Count = x.Count() })
            //**     .OrderBy(x => x.Year)
            //**     .ToArray();
            //** 
            //** var r1 = collection.Query()
            //**     .GroupBy(x => x.Date.Year)
            //**     .Select(x => new { x.Date.Year, Count = Sql.Count(x) })
            //**     .OrderBy(x => x.Year)
            //**     .ToArray();
            //** 
            //** foreach (var r in r0.Zip(r1, (l, r) => new { left = l, right = r }))
            //** {
            //**     Assert.AreEqual(r.left.Year, r.right.Year);
            //**     Assert.AreEqual(r.left.Count, r.right.Count);
            //** }
        }

        [TestMethod]
        public void Query_GroupBy_With_Array_Aggregation()
        {
            //** // quite complex group by query
            //** var r = collection.Query()
            //**     .GroupBy(x => x.Email.Substring(x.Email.IndexOf("@") + 1))
            //**     .Select(x => new
            //**     {
            //**         Domain = x.Email.Substring(x.Email.IndexOf("@") + 1),
            //**         Users = Sql.ToArray(new
            //**         {
            //**             Login = x.Email.Substring(0, x.Email.IndexOf("@")).ToLower(),
            //**             x.Name,
            //**             x.Age
            //**         })
            //**     })
            //**     .Limit(10)
            //**     .ToArray();
            //** 
            //** // test first only
            //** Assert.AreEqual(5, r[0].Users.Length);
            //** Assert.AreEqual("imperdiet.us", r[0].Domain);
            //** Assert.AreEqual("delilah", r[0].Users[0].Login);
            //** Assert.AreEqual("Dahlia Warren", r[0].Users[0].Name);
            //** Assert.AreEqual(24, r[0].Users[0].Age);
        }
    }
}