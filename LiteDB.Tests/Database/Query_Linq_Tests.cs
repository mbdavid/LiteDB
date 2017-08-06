using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace LiteDB.Tests.Database
{
    #region Model

    public enum PlatformID
    {
        Win32S,
        Win32Windows,
        Win32NT,
        WinCE,
        Unix,
        Xbox,
        MacOSX
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
        public int Age { get; set; }
        public PlatformID OS { get; set; }
        public UserDomain Domain { get; set; }

        public List<string> Names { get; set; }
        public List<UserDomain> Domains { get; set; }

        public override bool Equals(object obj)
        {
            var other = (obj as User);
            if (other != null)
            {
                return other.Id == this.Id;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
    }

    public class UserDomain
    {
        public string DomainName { get; set; }
        public int Age { get; set; }
    }

    #endregion

    [TestClass]
    public class Query_Linq_Tests
    {
        [TestMethod, TestCategory("Database")]
        public void Query_Linq_Visitor()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var c1 = new User { Id = 1, Name = "Mauricio", Active = true, Domain = new UserDomain { DomainName = "Numeria" }, OS = PlatformID.Xbox };
                var c2 = new User { Id = 2, Name = "Malatruco", Active = false, Domain = new UserDomain { DomainName = "Numeria" }, OS = PlatformID.Win32NT };
                var c3 = new User { Id = 3, Name = "Chris", Domain = new UserDomain { DomainName = "Numeria" }, OS = PlatformID.Win32NT };
                var c4 = new User { Id = 4, Name = "Juliane", OS = PlatformID.Win32NT };

                var col = db.GetCollection<User>("Customer");

                col.EnsureIndex(x => x.Name, true);
                col.EnsureIndex(x => x.OS, false);
                col.EnsureIndex(x => x.Domains.Select(z => z.DomainName), false);
                col.EnsureIndex(x => x.Domains[0].Age, false);
                //col.EnsureIndex(x => x.Active);

                var idx = col.GetIndexes().Select(x => x.Field).ToArray();

                Assert.AreEqual("Name", idx[1]);
                Assert.AreEqual("OS", idx[2]);
                Assert.AreEqual("Domains.DomainName", idx[3]);
                Assert.AreEqual("Domains.Age", idx[4]);

                col.Insert(new User[] { c1, c2, c3, c4 });

                // a simple lambda function to returns string "Numeria"
                Func<string> GetNumeria = () => "Numeria";
                var strNumeria = GetNumeria();


                // == !=
                Assert.AreEqual(1, col.Count(x => x.Id == 1));
                Assert.AreEqual(3, col.Count(x => x.Id != 1));

                // member booleans
                Assert.AreEqual(3, col.Count(x => !x.Active));
                Assert.AreEqual(1, col.Count(x => x.Active));

                // > >= < <=
                Assert.AreEqual(1, col.Count(x => x.Id > 3));
                Assert.AreEqual(1, col.Count(x => x.Id >= 4));
                Assert.AreEqual(1, col.Count(x => x.Id < 2));
                Assert.AreEqual(1, col.Count(x => x.Id <= 1));

                // sub-class
                Assert.AreEqual(3, col.Count(x => x.Domain.DomainName == "Numeria"));
                Assert.AreEqual(3, col.Count(x => x.Domain.DomainName == GetNumeria()));
                Assert.AreEqual(3, col.Count(x => x.Domain.DomainName == strNumeria));

                // methods
                Assert.AreEqual(1, col.Count(x => x.Name.StartsWith("Mal")));
                Assert.AreEqual(1, col.Count(x => x.Name.Equals("Mauricio")));
                Assert.AreEqual(1, col.Count(x => x.Name.Contains("cio")));

                // enum
                Assert.AreEqual(1, col.Count(x => x.OS == PlatformID.Xbox));
                Assert.AreEqual(1, col.Count(x => x.OS == (PlatformID)5)); // Xbox
                Assert.AreEqual(1, col.Count(x => x.OS == (PlatformID)Enum.Parse(typeof(PlatformID), "Xbox")));
                Assert.AreEqual(3, col.Count(x => x.OS == PlatformID.Win32NT));

                // and/or
                Assert.AreEqual(1, col.Count(x => x.Id > 0 && x.Name == "Mauricio"));
                Assert.AreEqual(2, col.Count(x => x.Name == "Malatruco" || x.Name == "Mauricio"));
            }
        }

        [TestMethod, TestCategory("Database")]
        public void Query_Linq_Enumerable()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var col = db.GetCollection<User>("Users");

                col.EnsureIndex(x => x.Name, true);
                col.EnsureIndex(x => x.Age);

                col.Insert(new[] { new User() { Id = 1, Name = "John Smith", Age = 10 },
                                   new User() { Id = 2, Name = "Jane Smith", Age = 12 },
                                   new User() { Id = 3, Name = "John Doe", Age = 24 },
                                   new User() { Id = 4, Name = "Jane Doe", Age = 42 } });

                var empty = new string[] { };
                Assert.AreEqual(0, col.Count(user => empty.All(name => user.Name.Contains(name))));
                Assert.AreEqual(0, col.Count(user => empty.Any(name => user.Name.Contains(name))));

                var firstNames = new[] { "John", "Jane", "Jon", "Janet" };
                Assert.AreEqual(0, col.Count(user => firstNames.All(name => user.Name.StartsWith(name))));
                Assert.AreEqual(4, col.Count(user => firstNames.Any(name => user.Name.StartsWith(name))));

                var surnames = new[] { "Smith", "Doe", "Mason", "Brown" };
                Assert.AreEqual(0, col.Count(user => surnames.All(name => user.Name.Contains(name))));
                Assert.AreEqual(4, col.Count(user => surnames.Any(name => user.Name.Contains(name))));

                var johnSmith = new[] { "John", "Smith" };
                Assert.AreEqual(1, col.Count(user => johnSmith.All(name => user.Name.Contains(name))));
                Assert.AreEqual(3, col.Count(user => johnSmith.Any(name => user.Name.Contains(name))));

                var janeDoe = new[] { "Jane", "Doe" };
                Assert.AreEqual(1, col.Count(user => janeDoe.All(name => user.Name.Contains(name))));
                Assert.AreEqual(3, col.Count(user => janeDoe.Any(name => user.Name.Contains(name))));

                var numRange = new[] { new { Min = 10, Max = 12 },
                                       new { Min = 21, Max = 33 } };
                var numQuery = numRange.Select(num => Query.And(Query.GTE("Age", num.Min), Query.LTE("Age", num.Max)));
                var queryResult = col.Find(numQuery.Aggregate((lhs, rhs) => Query.Or(lhs, rhs)));
                var lambdaResult = col.Find(p => numRange.Any(num => p.Age >= num.Min && p.Age <= num.Max));

                var seq1 = queryResult.OrderBy(u => u.Name);
                var seq2 = lambdaResult.OrderBy(u => u.Name);

                Assert.IsTrue(queryResult.OrderBy(u => u.Name).SequenceEqual(lambdaResult.OrderBy(u => u.Name)));
            }
        }

        [TestMethod, TestCategory("Database")]
        public void Query_Linq_Object()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var c1 = new User { Id = 1, Name = "Mauricio", Active = true, Domain = new UserDomain { DomainName = "Numeria" }, OS = PlatformID.Xbox };
                var c2 = new User { Id = 2, Name = "Malatruco", Active = false, Domain = new UserDomain { DomainName = "Numeria" }, OS = PlatformID.Win32NT };
                var c3 = new User { Id = 3, Name = "Chris", Domain = new UserDomain { DomainName = "Numeria" }, OS = PlatformID.Win32NT };
                var c4 = new User { Id = 4, Name = "Juliane", OS = PlatformID.Win32NT };

                var col = db.GetCollection<User>("Customer");

                col.EnsureIndex(x => x.Name);

                col.Insert(new [] { c1, c2, c3, c4 });

                // x.Name == "Mauricio" has index
                // x.Name.Length > 8 is Linq query
                var q1 = col.Find(x => x.Name == "Mauricio" && x.Name.Length > 5).ToArray();

                Assert.AreEqual(1, q1.Length);
                
                // it's not mapper ToUpper, must do LinqToObject
                var q2 = col.Find(x => x.Name.ToUpper() == "MAURICIO").ToArray();

                Assert.AreEqual(1, q1.Length);
            }
        }

    }
}