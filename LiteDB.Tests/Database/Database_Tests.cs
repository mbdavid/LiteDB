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

                Assert.AreEqual(2, users.Count());

                var carlos = users.Query()
                    .Where(x => x.Name == "Carlos")
                    //.Select(x => new { S = x.Salary })
                    .Single();

                Assert.AreEqual(75000, carlos.Salary);

                //Assert.AreEqual(75000, carlos.S);
            }
        }
    }
}