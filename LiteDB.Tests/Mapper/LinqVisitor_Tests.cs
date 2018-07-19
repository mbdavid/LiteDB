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

namespace LiteDB.Tests.Mapper
{
    [TestClass]
    public class LinqVisitor_Tests
    {
        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Address Address { get; set; }
            public float Salary { get; set; }
            public DateTime CreatedOn { get; set; }
            public List<Address> Addresses { get; set; }
            public bool Active { get; set; }

            public Address[] AddressesArray { get; set; }
            public IEnumerable<Address> AddressesEnumerable { get; set; }

        }

        public class Address
        {
            public string Street { get; set; }
            public int Number { get; set; }
            public City City { get; set; }
        }

        public class City
        {
            public string CityName { get; set; }
            public string Country { get; set; }
        }

        private BsonMapper _mapper = new BsonMapper();

        [TestMethod]
        public void Linq_Visitor_Expressions()
        {
            // date methods
            /*
            Test(x => x.CreatedOn.Year, "YEAR($.CreatedOn)");

            Test(x => x.CreatedOn.AddYears(2), "DATEADD('y',@p0,$.CreatedOn)");

            Test(x => x.CreatedOn.Month, "MONTH($.CreatedOn)");
            Test(x => x.CreatedOn.AddMonths(2), "DATEADD('M',@p0,$.CreatedOn)");
            Test(x => x.CreatedOn.AddDays(2), "DATEADD('d',@p0,$.CreatedOn)");

            Test(x => DateTime.Now, "DATE()");
            Test(x => DateTime.Now.Year, "YEAR(DATE())");
            */

            // document navigation
            Test(x => x.Id, "$._id");
            Test(x => x.Name, "$.Name");
            Test(x => x.Address.Street, "$.Address.Street");
            Test(x => x.Address.City.Country, "$.Address.City.Country");

            // some string methods access
            Test(x => x.Name.ToUpper(), "UPPER($.Name)");
            Test(x => x.Name.ToUpper().Trim(), "TRIM(UPPER($.Name))");
            Test(x => x.Name.ToUpper().Trim().PadLeft(5, '0'), "LPAD(TRIM(UPPER($.Name)),@p0,@p1)");
            Test(x => x.Name.Substring(0, 10), "SUBSTRING($.Name,@p0,@p1)");

            // accessing all items on array (use index [0] syntax) - can use ToList/ToArray to access
            Test(x => x.Addresses[0].Street, "$.Addresses[*].Street");
            Test(x => x.AddressesArray[0].Street, "$.AddressesArray[*].Street");
            Test(x => x.AddressesEnumerable.ToArray()[9].Street, "$.AddressesEnumerable[*].Street");
            Test(x => x.AddressesEnumerable.ToList()[9].Street, "$.AddressesEnumerable[*].Street");

            // accessing specific element on array (use ElementAt extension method)
            Test(x => x.Addresses.ElementAt(2).Street, "$.Addresses[@p0].Street");
            Test(x => x.AddressesArray.ElementAt(0).Street, "$.AddressesArray[@p0].Street");
            Test(x => x.AddressesEnumerable.ElementAt(2).Street, "$.AddressesEnumerable[@p0].Street");

            // conditionals
            Test(x => x.Salary > 50, "$.Salary>@p0");
            Test(x => x.Salary != 50, "$.Salary!=@p0");
            Test(x => x.Salary == x.Id, "$.Salary=$._id");
            Test(x => x.Salary > 50 && x.Name == "John", "$.Salary>@p0 AND $.Name=@p1");

        }

        private void Test<K>(Expression<Func<User, K>> predicate, string expect)
        {
            var expression = _mapper.GetExpression(predicate);

            Assert.AreEqual(expect, expression.Source);
        }
    }
}