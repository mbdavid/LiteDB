//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Collections;
//using System.Collections.Generic;
//using System.Text;
//using System.Reflection;
//using System.Text.RegularExpressions;
//using LiteDB.Engine;

//namespace LiteDB.Tests.Query
//{
//    [TestClass]
//    public class OrderBy_Tests
//    {
//        private LiteEngine db;
//        private BsonDocument[] person;

//        [TestInitialize]
//        public void Init()
//        {
//            db = new LiteEngine();
//            person = DataGen.Person(1, 20).ToArray();

//            db.Insert("person", person);
//            db.EnsureIndex("col", "name");
//        }

//        [TestCleanup]
//        public void CleanUp()
//        {
//            db.Dispose();
//        }

//        [TestMethod]
//        public void Query_OrderBy_Using_Index()
//        {
//            var r0 = person
//                .OrderBy(x => x["name"].AsString)
//                .Select(x => x["name"].AsString)
//                .ToArray();

//            var r1 = db.Query("person")
//                .OrderBy("name")
//                .Select("name")
//                .ToValues();

//            Assert.AreEqual(
//                string.Join(",", r0),
//                string.Join(",", r1.Select(x => x.AsString)));
//        }

//        [TestMethod]
//        public void Query_OrderBy_Using_Index_Desc()
//        {
//            var r0 = person
//                .OrderByDescending(x => x["name"].AsString)
//                .Select(x => x["name"].AsString)
//                .ToArray();

//            var r1 = db.Query("person")
//                .OrderBy("name", LiteDB.Query.Descending)
//                .Select("name")
//                .ToValues();

//            Assert.AreEqual(
//                string.Join(",", r0),
//                string.Join(",", r1.Select(x => x.AsString)));
//        }

//        [TestMethod]
//        public void Query_OrderBy_With_Func()
//        {
//            var r0 = person
//                .OrderBy(x => x["date"].AsDateTime.Day)
//                .Select(x => x["date"].AsDateTime.Day)
//                .ToArray();

//            var r1 = db.Query("person")
//                .OrderBy("DAY(date)")
//                .Select("DAY(date)")
//                .ToValues();

//            Assert.AreEqual(
//                string.Join(",", r0),
//                string.Join(",", r1.Select(x => x.AsInt32)));
//        }

//        [TestMethod]
//        public void Query_OrderBy_With_Offset_Limit()
//        {
//            var r0 = person
//                .OrderBy(x => x["date"].AsDateTime.Day)
//                .Skip(5)
//                .Take(10)
//                .Select(x => x["date"].AsDateTime.Day)
//                .ToArray();

//            var r1 = db.Query("person")
//                .OrderBy("DAY(date)")
//                .Offset(5)
//                .Limit(10)
//                .Select("DAY(date)")
//                .ToValues()
//                .ToArray();

//            Assert.AreEqual(
//                string.Join(",", r0),
//                string.Join(",", r1.Select(x => x.AsInt32)));
//        }
//    }
//}