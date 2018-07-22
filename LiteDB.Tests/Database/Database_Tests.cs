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
    public class Database_Tests
    {
        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public float Salary { get; set; }
            public bool Active { get; set; }
        }

        [TestMethod]
        public void Simple_Database_Insert()
        {
            using (var tmp = new TempFile())
            using (var db = new LiteDatabase(tmp.Filename))
            {
                var users = db.GetCollection<User>("users");

                users.Insert(new User { Name = "John", Salary = 128000, Active = true });
                users.Insert(new User { Name = "Carlos", Salary = 75000, Active = true });
                users.Insert(new User { Name = "John", Salary = 65000, Active = false });

                Assert.AreEqual(3, users.Count());

                var carlos = users.Query()
                    .Where(x => x.Name == "Carlos")
                    .Select(x => new { SAL = x.Salary, FullName = new { First = x.Name, Last = "Smith" } })
                    .Single();

                Assert.AreEqual(75000, carlos.SAL);
                Assert.AreEqual("Carlos", carlos.FullName.First);
                Assert.AreEqual("Smith", carlos.FullName.Last);

                var total = users.Query()
                    .Where(x => x.Active == true)
                    .SelectAll(x => Sql.Sum(x.Salary))
                    .ExecuteScalar();

                Assert.AreEqual(128000 + 75000, total);

                var group = users.Query()
                    .GroupBy(x => x.Active)
                    .Select(x => new { x.Active, Count = Sql.Count(x.Id), Salary = Sql.Sum(x.Salary) })
                    .ToArray();

            }
        }
    }
}