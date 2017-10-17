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
    }

    public class UserDomain
    {
        public string DomainName { get; set; }
        public int Age { get; set; }
    }

    #endregion

    [TestClass]
    public class Linq_Visitor_Tests
    {
        [TestMethod]
        public void Linq_Visitor_With_DbRef()
        {
            var m = new BsonMapper();

            m.Entity<UserDomain>()
                .Id(x => x.DomainName);

            m.Entity<User>()
                .DbRef(x => x.Domain)
                .Field(x => x.Domain, "current-domain")
                .DbRef(x => x.Domains);

            // Json PATH

            Assert.AreEqual("$.Age", m.GetPath<User>(x => x.Age));
            Assert.AreEqual("$._id", m.GetPath<User>(x => x.Id));
            Assert.AreEqual("$.current-domain", m.GetPath<User>(x => x.Domain));
            Assert.AreEqual("$.Domains[*]", m.GetPath<User>(x => x.Domains));
            Assert.AreEqual("$.Domains[*].Age", m.GetPath<User>(x => x.Domains[0].Age));
            Assert.AreEqual("$.Domains[*].Age", m.GetPath<User>(x => x.Domains.Select(z => z.Age)));
            Assert.AreEqual("$.Domains[*].$id", m.GetPath<User>(x => x.Domains[0].DomainName));

            // Bson Field
            Assert.AreEqual("Domains", m.GetField<User>(x => x.Domains));
            Assert.AreEqual("Domains.Age", m.GetField<User>(x => x.Domains[0].Age));
            Assert.AreEqual("Domains.$id", m.GetField<User>(x => x.Domains[0].DomainName));

            // Query
            Assert.AreEqual("(_id = 123)", m.GetQuery<User>(x => x.Id == 123).ToString());
            Assert.AreEqual("(_id between [1 and 2])", m.GetQuery<User>(x => x.Id >= 1 && x.Id <= 2).ToString());
            Assert.AreEqual("(Domains.$id = \"admin\")", m.GetQuery<User>(x => x.Domains[0].DomainName == "admin").ToString());
        }

        [TestMethod]
        public void Linq_Visitor_Without_DbRef()
        {
            var m = new BsonMapper();
            m.UseLowerCaseDelimiter();

            // Json PATH

            Assert.AreEqual("$.age", m.GetPath<User>(x => x.Age));
            Assert.AreEqual("$._id", m.GetPath<User>(x => x.Id));
            Assert.AreEqual("$.domain", m.GetPath<User>(x => x.Domain));
            Assert.AreEqual("$.domains[*]", m.GetPath<User>(x => x.Domains));
            Assert.AreEqual("$.domains[*].age", m.GetPath<User>(x => x.Domains[0].Age));
            Assert.AreEqual("$.domains[*].age", m.GetPath<User>(x => x.Domains.Select(z => z.Age)));
            Assert.AreEqual("$.domains[*].domain_name", m.GetPath<User>(x => x.Domains[0].DomainName));

            // Bson Field
            Assert.AreEqual("domains", m.GetField<User>(x => x.Domains));
            Assert.AreEqual("domains.age", m.GetField<User>(x => x.Domains[0].Age));
            Assert.AreEqual("domains.domain_name", m.GetField<User>(x => x.Domains[0].DomainName));

            // Query
            Assert.AreEqual("(_id = 123)", m.GetQuery<User>(x => x.Id == 123).ToString());
            Assert.AreEqual("(_id between [1 and 2])", m.GetQuery<User>(x => x.Id >= 1 && x.Id <= 2).ToString());
            Assert.AreEqual("(domains.domain_name = \"admin\")", m.GetQuery<User>(x => x.Domains[0].DomainName == "admin").ToString());
        }
    }
}