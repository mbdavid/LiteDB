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
    public class Linq_Query_Tests
    {
        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string[] Phones { get; set; }
            public Category Category { get; set; }
            public bool Active { get; set; }
        }

        public enum Category { Normal, Admin }

        [TestMethod]
        public void Linq_GroupBy_Query()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var users = db.GetCollection<User>("users");

                users.Insert(new User { Name = "John", Phones = new string[] { "555-9922" }, Category = Category.Admin, Active = true });
                users.Insert(new User { Name = "Papa", Phones = new string[] { "555-9922" }, Category = Category.Admin, Active = false });
                users.Insert(new User { Name = "Baby", Phones = new string[] { }, Category = Category.Normal, Active = true });
                users.Insert(new User { Name = "Marco", Phones = new string[] { "220-3011", "445-8800" }, Category = Category.Normal, Active = true });
                users.Insert(new User { Name = "Marco", Phones = new string[] { "220-5500", "555-0000" }, Category = Category.Normal, Active = false });

                // active users
                var active = users.Query()
                    .Where(x => x.Active == true)
                    .ToArray();

                Assert.AreEqual(3, active.Length);

                // has phone starts wtih 555
                var phones = users.Query()
                    .Where(x => x.Phones.Items().StartsWith("555"))
                    .OrderByDescending(x => x.Id)
                    .Select(x => new { FirstName = x.Name, PhoneCount = Sql.Count(x.Phones.Items()) })
                    .ToArray();

                Assert.AreEqual("Marco", phones[0].FirstName);
                Assert.AreEqual(2, phones[0].PhoneCount);

                // array of int phones
                var arrp = users.Query()
                    .Select(x => new { x.Name, Arr = Sql.ToArray(Convert.ToInt32(x.Phones.Items().Substring(0, 3))) })
                    .ToList();

                Assert.AreEqual(555, arrp[0].Arr[0]);
            }
        }
    }
}