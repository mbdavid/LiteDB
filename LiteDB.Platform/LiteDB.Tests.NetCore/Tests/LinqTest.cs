using System;
using System.IO;
using System.Linq;
using LiteDB.Tests.NetCore;

namespace LiteDB.Tests
{
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

    public class LinqTest : TestBase
    {
        public void Linq_Test()
        {
            var test_name = "Linq_Test";
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
                Helper.AssertIsTrue(test_name, 0, 3 == col.Count(x => x.Domain.DomainName == "Numeria"));
                Helper.AssertIsTrue(test_name, 1, 3 == col.Count(x => x.Domain.DomainName == GetNumeria()));
                Helper.AssertIsTrue(test_name, 2, 3 == col.Count(x => x.Domain.DomainName == strNumeria));

                // == !=
                Helper.AssertIsTrue(test_name, 3, 1 == col.Count(x => x.Id == 1));
                Helper.AssertIsTrue(test_name, 4, 3 == col.Count(x => x.Id != 1));

                // member booleans
                Helper.AssertIsTrue(test_name, 5, 3 == col.Count(x => !x.Active));
                Helper.AssertIsTrue(test_name, 6, 1 == col.Count(x => x.Active));

                // methods
                Helper.AssertIsTrue(test_name, 7, 1 == col.Count(x => x.Name.StartsWith("mal")));
                Helper.AssertIsTrue(test_name, 8, 1 == col.Count(x => x.Name.Equals("Mauricio")));
                Helper.AssertIsTrue(test_name, 9, 1 == col.Count(x => x.Name.Contains("cio")));

                // > >= < <=
                Helper.AssertIsTrue(test_name, 10, 1 == col.Count(x => x.Id > 3));
                Helper.AssertIsTrue(test_name, 11, 1 == col.Count(x => x.Id >= 4));
                Helper.AssertIsTrue(test_name, 12, 1 == col.Count(x => x.Id < 2));
                Helper.AssertIsTrue(test_name, 13, 1 == col.Count(x => x.Id <= 1));

                // enum
                Helper.AssertIsTrue(test_name, 14, 1 == col.Count(x => x.OS == PlatformID.Xbox));
                Helper.AssertIsTrue(test_name, 15, 1 == col.Count(x => x.OS == (PlatformID)5)); // Xbox
                Helper.AssertIsTrue(test_name, 16, 1 == col.Count(x => x.OS == (PlatformID)Enum.Parse(typeof(PlatformID), "Xbox")));
                Helper.AssertIsTrue(test_name, 17, 3 == col.Count(x => x.OS == PlatformID.Win32NT));

                // doesnt works... must be a better linq provider
                //var Platforms = new PlatformID[] { PlatformID.Xbox, PlatformID.Win32NT };
                //Helper.AssertIsTrue(test_name, 0, 4, col.Count(x => Platforms.Contains(x.OS)));


                // and/or
                Helper.AssertIsTrue(test_name, 18, 1 == col.Count(x => x.Id > 0 && x.Name == "MAURICIO"));
                Helper.AssertIsTrue(test_name, 19, 2 == col.Count(x => x.Name == "malatruco" || x.Name == "MAURICIO"));
            }
        }

        public void EnumerableTest()
        {
            var test_name = "EnumerableTest";
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
                Helper.AssertIsTrue(test_name, 1, 0 == col.Count(user => empty.All(name => user.Name.Contains(name))));
                Helper.AssertIsTrue(test_name, 2, 0 == col.Count(user => empty.Any(name => user.Name.Contains(name))));

                var firstNames = new[] { "John", "Jane", "Jon", "Janet" };
                Helper.AssertIsTrue(test_name, 3, 0 == col.Count(user => firstNames.All(name => user.Name.StartsWith(name))));
                Helper.AssertIsTrue(test_name, 4, 4 == col.Count(user => firstNames.Any(name => user.Name.StartsWith(name))));

                var surnames = new[] { "Smith", "Doe", "Mason", "Brown" };
                Helper.AssertIsTrue(test_name, 5, 0 == col.Count(user => surnames.All(name => user.Name.Contains(name))));
                Helper.AssertIsTrue(test_name, 6, 4 == col.Count(user => surnames.Any(name => user.Name.Contains(name))));

                var johnSmith = new[] { "John", "Smith" };
                Helper.AssertIsTrue(test_name, 7, 1 == col.Count(user => johnSmith.All(name => user.Name.Contains(name))));
                Helper.AssertIsTrue(test_name, 8, 3 == col.Count(user => johnSmith.Any(name => user.Name.Contains(name))));

                var janeDoe = new[] { "Jane", "Doe" };
                Helper.AssertIsTrue(test_name, 10, 1 == col.Count(user => janeDoe.All(name => user.Name.Contains(name))));
                Helper.AssertIsTrue(test_name, 11, 3 == col.Count(user => janeDoe.Any(name => user.Name.Contains(name))));

                var numRange = new[] { new { Min = 10, Max = 12 },
                                       new { Min = 21, Max = 33 } };
                var numQuery = numRange.Select(num => Query.And(Query.GTE("Age", num.Min), Query.LTE("Age", num.Max)));
                var queryResult = col.Find(numQuery.Aggregate((lhs, rhs) => Query.Or(lhs, rhs)));
                var lambdaResult = col.Find(p => numRange.Any(num => p.Age >= num.Min && p.Age <= num.Max));

                var seq1 = queryResult.OrderBy(u => u.Name);
                var seq2 = lambdaResult.OrderBy(u => u.Name);

                Helper.AssertIsTrue(test_name, 12, queryResult.OrderBy(u => u.Name).SequenceEqual(lambdaResult.OrderBy(u => u.Name)));
            }
        }
    }
}