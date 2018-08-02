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

namespace LiteDB.Tests.Database
{
    [TestClass]
    public class Aggregate_Tests
    {
        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public float Salary { get; set; }
            public bool Active { get; set; }
        }

        [TestMethod]
        public void Aggregates()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var users = db.GetCollection<User>("users");

                users.Insert(new User { Name = "John", Salary = 128000, Active = true });
                users.Insert(new User { Name = "Zarlos", Salary = 75000, Active = true });
                users.Insert(new User { Name = "Ana", Salary = 65000, Active = false });

                Assert.AreEqual(3, users.Count());
                Assert.AreEqual(2, users.Count("Active = true")); // must be a predicate
                Assert.AreEqual(2, users.Count(x => x.Active == true)); 

                Assert.AreEqual(3L, users.LongCount());
                Assert.AreEqual(1L, users.LongCount("Salary > 100000"));
                Assert.AreEqual(1L, users.LongCount(x => x.Salary > 100000));

                Assert.AreEqual("Zarlos", users.Max(x => x.Name));
                Assert.AreEqual("Ana", users.Min(x => x.Name));

                Assert.IsTrue(users.Exists(x => x.Salary == 75000));
            }
        }
    }
}