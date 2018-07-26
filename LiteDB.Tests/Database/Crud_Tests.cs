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
    public class Crud_Tests
    {
        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [TestMethod]
        public void Insert_With_AutoId()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var users = db.GetCollection<User>("users");

                var u1 = new User { Name = "John" };
                var u2 = new User { Name = "Zarlos" };
                var u3 = new User { Name = "Ana" };

                // insert ienumerable
                users.Insert(new User[] { u1, u2 });

                users.Insert(u3);

                // test auto-id
                Assert.AreEqual(1, u1.Id);
                Assert.AreEqual(2, u2.Id);
                Assert.AreEqual(3, u3.Id);

                // adding without autoId
                var u4 = new User { Id = 20, Name = "Marco" };

                users.Insert(u4);

                // adding more auto id after fixed id
                var u5 = new User { Name = "Julio" };

                users.Insert(u5);

                Assert.AreEqual(21, u5.Id);
            }
        }
    }
}