using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
        public int Age { get; set; }
        public PlatformID OS { get; set; }

        public UserDomain Domain { get; set; }

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
    }

    [TestClass]
    public class LinqTest
    {
        [TestMethod]
        public void Linq_Test()
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

                col.Insert(new User[] { c1, c2, c3, c4 });

                // a simple lambda function to returns string "Numeria"
                Func<string> GetNumeria = () => "Numeria";
                var strNumeria = GetNumeria();

                // sub-class
                Assert.AreEqual(3, col.Count(x => x.Domain.DomainName == "Numeria"));
                Assert.AreEqual(3, col.Count(x => x.Domain.DomainName == GetNumeria()));
                Assert.AreEqual(3, col.Count(x => x.Domain.DomainName == strNumeria));
                
                // == !=
                Assert.AreEqual(1, col.Count(x => x.Id == 1));
                Assert.AreEqual(3, col.Count(x => x.Id != 1));
                
                // member booleans
                Assert.AreEqual(3, col.Count(x => !x.Active));
                Assert.AreEqual(1, col.Count(x => x.Active));
                
                // methods
                Assert.AreEqual(1, col.Count(x => x.Name.StartsWith("mal")));
                Assert.AreEqual(1, col.Count(x => x.Name.Equals("Mauricio")));
                Assert.AreEqual(1, col.Count(x => x.Name.Contains("cio")));
                
                // > >= < <=
                Assert.AreEqual(1, col.Count(x => x.Id > 3));
                Assert.AreEqual(1, col.Count(x => x.Id >= 4));
                Assert.AreEqual(1, col.Count(x => x.Id < 2));
                Assert.AreEqual(1, col.Count(x => x.Id <= 1));

                // enum
                Assert.AreEqual(1, col.Count(x => x.OS == PlatformID.Xbox));
                Assert.AreEqual(1, col.Count(x => x.OS == (PlatformID)5)); // Xbox
                Assert.AreEqual(1, col.Count(x => x.OS == (PlatformID)Enum.Parse(typeof(PlatformID), "Xbox")));
                Assert.AreEqual(3, col.Count(x => x.OS == PlatformID.Win32NT));

                // doesnt works... must be a better linq provider
                //var plataforms = new PlatformID[] { PlatformID.Xbox, PlatformID.Win32NT };
                //Assert.AreEqual(4, col.Count(x => plataforms.Contains(x.OS)));


                // and/or
                Assert.AreEqual(1, col.Count(x => x.Id > 0 && x.Name == "MAURICIO"));
                Assert.AreEqual(2, col.Count(x => x.Name == "malatruco" || x.Name == "MAURICIO"));
            }
        }

        [TestMethod]
        public void EnumerableTest()
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
                Assert.IsTrue(queryResult.OrderBy(u => u.Name).SequenceEqual(lambdaResult.OrderBy(u => u.Name)));
            }
        }
    }
}