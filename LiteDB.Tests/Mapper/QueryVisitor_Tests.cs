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
    public class QueryVisitor_Tests
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

            public string InvalidMethod() => "will thow error in eval";
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

        private static Address StaticProp { get; set; } = new Address { Number = 99 };
        private const int CONST_INT = 100;
        private string MyMethod() => "ok";
        private int MyIndex() => 5;

        [TestMethod]
        public void Linq_Document_Navigation()
        {
            // document navigation
            Test(x => x.Id, "_id");
            Test(x => x.Name, "Name");
            Test(x => x.Address.Street, "Address.Street");
            Test(x => x.Address.City.Country, "Address.City.Country");
        }

        [TestMethod]
        public void Linq_Constants()
        {
            // only constants
            var today = DateTime.Today;
            var john = "JOHN";
            var a = new { b = new { c = "JOHN" } };

            // only constants
            Test(x => 0, "@p0", 0);
            Test(x => 1 + 1, "@p0", 2); // "1 + 1" will be resolved by LINQ before convert

            // values from variables
            Test(x => today, "@p0", today);

            // values from deep object variables
            Test(x => a.b.c, "@p0", a.b.c);
            Test(x => x.Address.Street == a.b.c, "Address.Street = @p0", a.b.c);

            // class constants
            Test(x => CONST_INT, "@p0", CONST_INT);
            Test(x => QueryVisitor_Tests.StaticProp.Number, "@p0", QueryVisitor_Tests.StaticProp.Number);

            // methods inside constants
            Test(x => "demo".Trim(), "TRIM(@p0)", "demo");

            // execute method inside variables
            Test(x => john.Trim(), "TRIM(@p0)", john);
            Test(x => today.Day, "DAY(@p0)", today);

            // testing node stack using parameter-expression vs variable-expression
            Test(x => x.Name.Length > john.Length, "LENGTH(Name) > LENGTH(@p0)", john);

            // calling external methods
            Test(x => x.Name == MyMethod(), "Name = @p0", MyMethod());
            Test(x => MyMethod().Length, "LENGTH(@p0)", MyMethod());

            try
            {
                Test(x => x.Name == x.Address.InvalidMethod(), "Name = Address");
                Assert.Fail("must throw error when call parameter expression method");
            }
            catch(NotSupportedException)
            {
            }
        }

        [TestMethod]
        public void Linq_Array_Access()
        {
            // new `Items` array access
            Test(x => x.Phones.Items().Number, "Phones[*].Number");
            Test(x => x.Phones.Items(1).Number, "Phones[@p0].Number", 1);
            Test(x => x.Phones.Items(z => z.Prefix == 0).Number, "Phones[@.Prefix = @p0].Number", 0);

            // access using native array index
            Test(x => x.Phones[1].Number, "Phones[@p0].Number", 1);

            // fixed position
            Test(x => x.Phones[15], "Phones[@p0]", 15);
            Test(x => x.Phones.ElementAt(1), "Phones[@p0]", 1);

            // fixed position based on method names
            Test(x => x.Phones.First(), "Phones[0]");
            Test(x => x.Phones.Last(), "Phones[-1]");

            // call external method/props/const inside parameter expression
            var a = new { b = new { c = 123 } };

            Test(x => x.Phones.Items(a.b.c).Number, "Phones[@p0].Number", a.b.c);
            Test(x => x.Phones.Items(CONST_INT).Number, "Phones[@p0].Number", CONST_INT);
            Test(x => x.Phones.Items(MyIndex()).Number, "Phones[@p0].Number", MyIndex());
        }


        [TestMethod]
        public void Linq_Predicate()
        {
            // unary expressions
            Test(x => x.Active, "Active = true");
            Test(x => x.Active == true, "Active = @p0", true);
            Test(x => x.Active && true, "Active AND @p0", true);
            Test(x => !x.Active, "(Active) = false");

            // binary expressions
            Test(x => x.Salary > 50, "Salary > @p0", 50);
            Test(x => x.Salary != 50, "Salary != @p0", 50);
            Test(x => x.Salary == x.Id, "Salary = _id");
            Test(x => x.Salary > 50 && x.Name == "John", "Salary > @p0 AND Name = @p1", 50, "John");

            // iif (c ? true : false)
            Test(x => x.Id > 10 ? x.Id : 0, "IIF(_id > @p0, _id, @p1)", 10, 0);
        }

        [TestMethod]
        public void Linq_Cast_Convert_Types()
        {
            // cast only fromType Double/Decimal to Int32/64

            // int cast/convert/parse
            Test(x => (int)123.44, "TO_INT(@p0)", 123.44);
            Test(x => (int)123.99m, "TO_INT(@p0)", 123.99m);
            Test(x => Convert.ToInt32("123"), "TO_INT(@p0)", "123");
            Test(x => Int32.Parse("123"), "TO_INT(@p0)", "123");
        }

        [TestMethod]
        public void Linq_Methods()
        {
            // string instance methods
            Test(x => x.Name.ToUpper(), "UPPER(Name)");
            Test(x => x.Name.Substring(10), "SUBSTRING(Name, @p0)", 10);
            Test(x => x.Name.Substring(10, 20), "SUBSTRING(Name, @p0, @p1)", 10, 20);
            Test(x => x.Name.Replace('+', '-'), "REPLACE(Name, @p0, @p1)", "+", "-");
            Test(x => x.Name.IndexOf("m"), "INDEXOF(Name, @p0)", "m");
            Test(x => x.Name.IndexOf("m", 20), "INDEXOF(Name, @p0, @p1)", "m", 20);
            Test(x => x.Name.ToUpper().Trim(), "TRIM(UPPER(Name))");
            Test(x => x.Name.ToUpper().Trim().PadLeft(5, '0'), "LPAD(TRIM(UPPER($.Name)), @p0, @p1)", 5, "0");

            // string LIKE
            Test(x => x.Name.StartsWith("Mauricio"), "Name LIKE (@p0 + '%')", "Mauricio");
            Test(x => x.Name.Contains("Bezerra"), "Name LIKE ('%' + @p0 + '%')", "Bezerra");
            Test(x => x.Name.EndsWith("David"), "Name LIKE ('%' + @p0)", "David");
            Test(x => x.Name.StartsWith(x.Address.Street), "Name LIKE (Address.Street + '%')");

            // string members
            Test(x => x.Address.Street.Length, "LENGTH(Address.Street)");
            Test(x => string.Empty, "''");

            // string static methods
            Test(x => string.IsNullOrEmpty(x.Name), "(Name = null OR LENGTH(Name) = 0)");
            Test(x => string.IsNullOrWhiteSpace(x.Name), "(Name = null OR LENGTH(TRIM(Name)) = 0)");

            // guid initialize/converter
            Test(x => Guid.NewGuid(), "GUID()");
            Test(x => Guid.Empty, "TO_GUID('00000000-0000-0000-0000-000000000000')");
            Test(x => Guid.Parse("1A3B944E-3632-467B-A53A-206305310BAC"), "TO_GUID(@p0)", "1A3B944E-3632-467B-A53A-206305310BAC");

            // toString/Format
            Test(x => x.Id.ToString(), "TO_STRING(_id)");
            Test(x => x.CreatedOn.ToString(), "TO_STRING(CreatedOn)");
            Test(x => x.CreatedOn.ToString("yyyy-MM-dd"), "FORMAT(CreatedOn, @p0)", "yyyy-MM-dd");
            Test(x => x.Salary.ToString("#.##0,00"), "FORMAT(Salary, @p0)", "#.##0,00");

            // adding dates
            Test(x => x.CreatedOn.AddYears(5), "DATEADD('y', @p0, CreatedOn)", 5);
            Test(x => x.CreatedOn.AddMonths(5), "DATEADD('M', @p0, CreatedOn)", 5);
            Test(x => x.CreatedOn.AddDays(5), "DATEADD('d', @p0, CreatedOn)", 5);
            Test(x => x.CreatedOn.AddHours(5), "DATEADD('h', @p0, CreatedOn)", 5);
            Test(x => x.CreatedOn.AddMinutes(5), "DATEADD('m', @p0, CreatedOn)", 5);
            Test(x => x.CreatedOn.AddSeconds(5), "DATEADD('s', @p0, CreatedOn)", 5);

            // using dates properties
            Test(x => x.CreatedOn.Year, "YEAR(CreatedOn)");
            Test(x => x.CreatedOn.Month, "MONTH(CreatedOn)");
            Test(x => x.CreatedOn.Day, "DAY(CreatedOn)");
            Test(x => x.CreatedOn.Hour, "HOUR(CreatedOn)");
            Test(x => x.CreatedOn.Minute, "MINUTE(CreatedOn)");
            Test(x => x.CreatedOn.Second, "SECOND(CreatedOn)");

            // static date
            Test(x => DateTime.Now, "NOW()");
            Test(x => DateTime.UtcNow, "NOW_UTC()");
            Test(x => DateTime.Today, "TODAY()");
        }

        [TestMethod]
        public void Linq_Sql_Methods()
        {
            // extensions methods aggregation (can change name)
            Test(x => Sql.Sum(x.Phones.Items().Number), "SUM(Phones[*].Number)");
            Test(x => Sql.Max(x.Phones.Items()), "MAX(Phones[*])");
            Test(x => Sql.Count(x.Phones.Items().Number), "COUNT(Phones[*].Number)");
        }

        [TestMethod]
        public void Linq_New_Instance()
        {
            // new class
            Test(x => new { x.Name, x.Address }, "{ Name, Address }");
            Test(x => new { N = x.Name, A = x.Address }, "{ N: $.Name, A: $.Address }");

            // new array
            Test(x => new int[] { x.Id, 6, 7 }, "[_id, @p0, @p1]", 6, 7);

            // new fixed types
            Test(x => new DateTime(2018, 5, 28), "TO_DATETIME(@p0, @p1, @p2)", 2018, 5, 28);

            Test(x => new Guid("1A3B944E-3632-467B-A53A-206305310BAC"), "TO_GUID(@p0)", "1A3B944E-3632-467B-A53A-206305310BAC");

        }

        [TestMethod]
        public void Linq_Complex_Expressions()
        {
            // 'CityName': $.Address.City.CityName, 
            // 'Cnt': COUNT(IIF(TO_STRING($.Phones[@.Type = @p0].Number) = @p1, @p2, $.Name)), 
            // 'List': TO_ARRAY($.Phones[*].Number + $.Phones[@.Prefix > $.Salary].Number)

            Test(x => new
            {
                x.Address.City.CityName,
                Cnt = Sql.Count(x.Phones.Items(z => z.Type == PhoneType.Landline).Number.ToString() == "555" ? MyMethod() : x.Name),
                List = Sql.ToList(x.Phones.Items().Number + x.Phones.Items(z => z.Prefix > x.Salary).Number)
            },
            @"
            {
                CityName: $.Address.City.CityName,
                Cnt: COUNT(IIF(TO_STRING($.Phones[@.Type = @p0].Number) = @p1, @p2, $.Name)),
                List: TO_ARRAY($.Phones[*].Number + $.Phones[@.Prefix > $.Salary].Number)    
            }", 
            (int)PhoneType.Landline, "555", MyMethod());
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

                Assert.AreEqual(par, pval, "Expression: " + expect.Source);
            }

            return expression;
        }
    }
}