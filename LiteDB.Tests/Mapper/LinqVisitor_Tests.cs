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
            public double Salary { get; set; }
            public DateTime CreatedOn { get; set; }
            public bool Active { get; set; }

            public Address Address { get; set; }
            public List<Phone> Phones { get; set; }
            public Phone[] Phones2 { get; set; }
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

        public class Phone
        {
            public PhoneType Type { get; set; }
            public int Prefix { get; set; }
            public int Number { get; set; }
        }

        public enum PhoneType
        {
            Mobile, Landline
        }

        private BsonMapper _mapper = new BsonMapper();

        [TestMethod]
        public void Linq_Visitor_Expressions()
        {

            // guid initialize/converter
            Test(x => Guid.NewGuid(), "GUID()");
            Test(x => Guid.Empty, "GUID('00000000-0000-0000-0000-000000000000')");
            Test(x => Guid.Parse("1A3B944E-3632-467B-A53A-206305310BAE"), "GUID(@p0)", "1A3B944E-3632-467B-A53A-206305310BAE");

            


            // adding dates
            Test(x => x.CreatedOn.AddYears(5), "DATEADD('y', @p0, CreatedOn)", 5);
            Test(x => x.CreatedOn.AddMonths(5), "DATEADD('M', @p0, CreatedOn)", 5);
            Test(x => x.CreatedOn.AddDays(5), "DATEADD('d', @p0, CreatedOn)", 5);
            Test(x => x.CreatedOn.AddHours(5), "DATEADD('h', @p0, CreatedOn)", 5);
            Test(x => x.CreatedOn.AddMinutes(5), "DATEADD('m', @p0, CreatedOn)", 5);
            Test(x => x.CreatedOn.AddSeconds(5), "DATEADD('s', @p0, CreatedOn)", 5);


            // using dates methods/properties
            Test(x => x.CreatedOn.Day, "DAY(CreatedOn)");

            // static date
            Test(x => DateTime.Now, "DATE()");
            Test(x => DateTime.UtcNow, "DATE_UTC()");

            // extensions methods aggregation (can change name)
            Test(x => Sql.Sum(x.Phones.Index().Number), "SUM(Phones[*].Number)");
            Test(x => Sql.Max(x.Phones.Index()), "MAX(Phones[*])");
            Test(x => Sql.Count(x.Phones.Index().Number), "COUNT(Phones[*].Number)");

            // new `Arr` array access
            Test(x => x.Phones.Index().Number, "Phones[*].Number");
            Test(x => x.Phones.Index(1).Number, "Phones[@p0].Number", 1);
            Test(x => x.Phones.Index(z => z.Prefix == 0).Number, "Phones[@.Prefix = @p0].Number", 0);

            // access using native array index
            Test(x => x.Phones[1].Number, "Phones[@p0].Number", 1);

            // fixed position
            Test(x => x.Phones[15], "Phones[@p0]", 15);
            Test(x => x.Phones.ElementAt(1), "Phones[@p0]", 1);

            // fixed position based on method names
            Test(x => x.Phones.First(), "Phones[0]");
            Test(x => x.Phones.Last(), "Phones[-1]");

            return;

            // string instance methods
            Test(x => x.Name.ToUpper(), "UPPER(Name)");
            Test(x => x.Name.Substring(10), "SUBSTRING(Name, @p0)", 10);
            Test(x => x.Name.Substring(10, 20), "SUBSTRING(Name, @p0, @p1)", 10, 20);
            Test(x => x.Name.Replace('+', '-'), "REPLACE(Name, @p0, @p1)", "+", "-");
            Test(x => x.Name.IndexOf("m"), "INDEXOF(Name, @p0)", "m");
            Test(x => x.Name.IndexOf("m", 20), "INDEXOF(Name, @p0, @p1)", "m", 20);

            // string LIKE
            Test(x => x.Name.StartsWith("Mauricio"), "Name LIKE (@p0 + '%')", "Mauricio");
            Test(x => x.Name.Contains("Bezerra"), "Name LIKE ('%' + @p0 + '%')", "Bezerra");
            Test(x => x.Name.EndsWith("David"), "Name LIKE ('%' + @p0)", "David");
            Test(x => x.Name.StartsWith(x.Address.Street), "Name LIKE (Address.Street + '%')");

            // string length member
            Test(x => x.Address.Street.Length, "LENGTH(Address.Street)");

            // string static methods
            Test(x => string.IsNullOrEmpty(x.Name), "(Name = null OR LENGTH(Name) = 0)");
            Test(x => string.IsNullOrWhiteSpace(x.Name), "(Name = null OR LENGTH(TRIM(Name)) = 0)");

            // new class
            Test(x => new { x.Name, x.Address }, "{ Name, Address }");
            Test(x => new { N = x.Name, A = x.Address }, "{ N: $.Name, A: $.Address }");

            // new array
            Test(x => new int[] { x.Id, 6, 7 }, "[_id, @p0, @p1]", 5, 6);

            // only constants
            Test(x => 0, "@p0", 0);
            Test(x => 1 + 1, "@p0", 2); // "1 + 1" will be resolved by LINQ before convert


            // document navigation
            Test(x => x.Id, "_id");
            Test(x => x.Name, "Name");
            Test(x => x.Address.Street, "Address.Street");
            Test(x => x.Address.City.Country, "Address.City.Country");

            // some string methods access
            Test(x => x.Name.ToUpper(), "UPPER(Name)");
            Test(x => x.Name.ToUpper().Trim(), "TRIM(UPPER(Name))");
            Test(x => x.Name.ToUpper().Trim().PadLeft(5, '0'), "LPAD(TRIM(UPPER($.Name)), @p0, @p1)", 5, "0");
            Test(x => x.Name.Substring(0, 10), "SUBSTRING($.Name, @p0, @p1)", 0, 10);

            //-- // accessing all items on array (use index [0] syntax) - can use ToList/ToArray to access
            //-- Test(x => x.Addresses[0].Street, "$.Addresses[*].Street");
            //-- Test(x => x.AddressesArray[0].Street, "$.AddressesArray[*].Street");
            //-- Test(x => x.AddressesEnumerable.ToArray()[9].Street, "$.AddressesEnumerable[*].Street");
            //-- Test(x => x.AddressesEnumerable.ToList()[9].Street, "$.AddressesEnumerable[*].Street");
            //-- 
            //-- // accessing specific element on array (use ElementAt extension method)
            //-- Test(x => x.Addresses.ElementAt(2).Street, "$.Addresses[@p0].Street");
            //-- Test(x => x.AddressesArray.ElementAt(0).Street, "$.AddressesArray[@p0].Street");
            //-- Test(x => x.AddressesEnumerable.ElementAt(2).Street, "$.AddressesEnumerable[@p0].Street");

            // conditionals
            Test(x => x.Salary > 50, "Salary > @p0", 50);
            Test(x => x.Salary != 50, "Salary != @p0", 50);
            Test(x => x.Salary == x.Id, "Salary = _id");
            Test(x => x.Salary > 50 && x.Name == "John", "Salary > @p0 AND Name = @p1", 50, "John");

            // unary
            Test(x => x.Active == true, "Active = @p0", true);
            Test(x => x.Active && true, "Active AND @p0", true);
            Test(x => !x.Active, "(Active) = false");

            // iif (c ? true : false)
            Test(x => x.Id > 10 ? x.Id : 0, "IIF(_id > @p0, _id, @p1)", 10, 0);

        }

        /// <summary>
        /// Compare LINQ expression with expected BsonExpression
        /// </summary>
        private BsonExpression Test<K>(Expression<Func<User, K>> predicate, BsonExpression expect, params BsonValue[] args)
        {
            var expression = _mapper.GetExpression(predicate);

            Assert.AreEqual(expect.Source, expression.Source);

            var index = 0;

            foreach(var par in args)
            {
                var pval = expression.Parameters["p" + (index++).ToString()];

                Assert.AreEqual(par, pval);
            }

            return expression;
        }
    }
}