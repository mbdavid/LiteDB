using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace LiteDB.Tests.Mapper
{
    [TestClass]
    public class LinqBsonExpression_Tests
    {
        #region Model

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

            [BsonField("USER_DOMAIN_NAME")]
            public string DomainName { get; }

            /// <summary>
            /// This type will be render as new BsonDoctument { [key] = value }
            /// </summary>
            public IDictionary<string, Address> MetaData { get; set; }
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

        private static Address StaticProp { get; set; } = new Address { Number = 99 };
        private const int CONST_INT = 100;
        private string MyMethod() => "ok";
        private int MyIndex() => 5;

        #endregion

        private BsonMapper _mapper = new BsonMapper();

        [TestMethod]
        public void Linq_Document_Navigation()
        {
            // document navigation
            TestExpr<User>(x => x.Id, "_id");
            TestExpr<User>(x => x.Name, "Name");
            TestExpr<User>(x => x.Address.Street, "Address.Street");
            TestExpr<User>(x => x.Address.City.Country, "Address.City.Country");
        }

        [TestMethod]
        public void Linq_Constants()
        {
            // only constants
            var today = DateTime.Today;
            var john = "JOHN";
            var a = new { b = new { c = "JOHN" } };

            // only constants
            TestExpr(x => 0, "@p0", 0);
            TestExpr(x => 1 + 1, "@p0", 2); // "1 + 1" will be resolved by LINQ before convert

            // values from variables
            TestExpr(x => today, "@p0", today);

            // values from deep object variables
            TestExpr(x => a.b.c, "@p0", a.b.c);
            TestExpr<User>(x => x.Address.Street == a.b.c, "(Address.Street = @p0)", a.b.c);

            // class constants
            TestExpr(x => CONST_INT, "@p0", CONST_INT);
            TestExpr(x => StaticProp.Number, "@p0", StaticProp.Number);

            // methods inside constants
            TestExpr(x => "demo".Trim(), "TRIM(@p0)", "demo");

            // execute method inside variables
            TestExpr(x => john.Trim(), "TRIM(@p0)", john);
            TestExpr(x => today.Day, "DAY(@p0)", today);

            // testing node stack using parameter-expression vs variable-expression
            TestExpr<User>(x => x.Name.Length > john.Length, "(LENGTH(Name) > LENGTH(@p0))", john);

            // calling external methods
            TestExpr<User>(x => x.Name == MyMethod(), "(Name = @p0)", MyMethod());
            TestExpr(x => MyMethod().Length, "LENGTH(@p0)", MyMethod());

            TestException<User, NotSupportedException>(() => x => x.Name == x.Address.InvalidMethod());
        }

        [TestMethod]
        public void Linq_Enumerables()
        {
            // access array items
            TestExpr<User>(x => x.Phones, "$.Phones");
            TestExpr<User>(x => x.Phones.AsEnumerable(), "$.Phones[*]");

            // where
            TestExpr<User>(x => x.Phones.Where(p => p.Prefix == 1), "$.Phones[(@.Prefix = @p0)]", 1);
            TestExpr<User>(x => x.Phones.Where(p => p.Prefix == x.Id), "$.Phones[(@.Prefix = $._id)]");

            // aggregate
            TestExpr<User>(x => x.Phones.Count(), "COUNT($.Phones)");
            TestExpr<User>(x => x.Phones.Min(), "MIN($.Phones)");
            TestExpr<User>(x => x.Phones.Max(), "MAX($.Phones)");
            TestExpr<User>(x => x.Phones.Select(p => p.Number).Sum(), "SUM(($.Phones => @.Number))");

            // aggregate with map
            TestExpr<User>(x => x.Phones.Sum(w => w.Number), "SUM($.Phones => @.Number)");

            // map
            TestExpr<User>(x => x.Phones.Select(y => y.Type), "($.Phones => @.Type)");

            TestExpr<User>(x => x.Phones.Select(p => p.Number).Sum(), "SUM(($.Phones => @.Number))");
            TestExpr<User>(x => x.Phones.Select(p => p.Number).Average(), "AVG(($.Phones => @.Number))");

            // array/list
            TestExpr<User>(x => x.Phones.Where(w => w.Number == 5).ToArray(), "ARRAY($.Phones[(@.Number = @p0)])", 5);
            TestExpr<User>(x => x.Phones.ToList(), "ARRAY($.Phones)");

            // access using native array index (special "get_Item" eval index value)
            TestExpr<User>(x => x.Phones[1].Number, "$.Phones[1].Number");

            // fixed position
            TestExpr<User>(x => x.Phones[15], "$.Phones[15]");
            TestExpr<User>(x => x.Phones.ElementAt(1), "$.Phones[@p0]", 1);

            // call external method/props/const inside parameter expression
            var a = new { b = new { c = 123 } };

            // Items(int) generate eval value index
            TestExpr<User>(x => x.Phones[a.b.c].Number, "$.Phones[123].Number");
            TestExpr<User>(x => x.Phones[CONST_INT].Number, "$.Phones[100].Number");
            TestExpr<User>(x => x.Phones[MyIndex()].Number, "$.Phones[5].Number");

            // fixed position
            TestExpr<User>(x => x.Phones.First(), "$.Phones[0]");
            TestExpr<User>(x => x.Phones.Last(), "$.Phones[-1]");

            // fixed position with filter expression
            TestExpr<User>(x => x.Phones.First(p => p.Number == 1), "FIRST($.Phones[(@.Number = @p0)])", 1);

            // using any/all
            TestExpr<User>(x => x.Phones.Select(p => p.Number).Any(p => p == 1), "(Phones => @.Number) ANY = @p0", 1);
            TestExpr<User>(x => x.Phones.Select(p => p.Number.ToString()).Any(p => p.StartsWith("51")),
                "(Phones => STRING(@.Number)) ANY LIKE (@p0 + '%')", "51");

        }

        [TestMethod]
        public void Linq_Predicate()
        {
            // binary expressions
            TestPredicate<User>(x => x.Active == true, "(Active = @p0)", true);
            TestPredicate<User>(x => x.Salary > 50, "(Salary > @p0)", 50);
            TestPredicate<User>(x => x.Salary != 50, "(Salary != @p0)", 50);
            TestPredicate<User>(x => x.Salary == x.Id, "(Salary = _id)");
            TestPredicate<User>(x => x.Salary > 50 && x.Name == "John", "((Salary > @p0) AND (Name = @p1))", 50, "John");

            // unary expressions
            TestPredicate<User>(x => x.Active, "(Active = true)");
            TestPredicate<User>(x => x.Active && x.Active, "((Active = true) AND (Active = true))");
            TestPredicate<User>(x => x.Active && x.Active && x.Active, "((($.Active=true) AND ($.Active=true)) AND ($.Active=true))");
            TestPredicate<User>(x => x.Active && !x.Active, "((Active = true) AND (Active = false))");
            TestPredicate<User>(x => !x.Active, "(Active = false)");
            TestPredicate<User>(x => !x.Active == true, "((Active = false) = @p0)", true);
            TestPredicate<User>(x => !(x.Salary == 50), "(Salary = @p0) = false", 50);

            // test for precedence order
            TestPredicate<User>(x => x.Name.StartsWith("J") == false, "(Name LIKE (@p0 + '%') = @p1)", "J", false);

            // iif (c ? true : false)
            TestExpr<User>(x => x.Id > 10 ? x.Id : 0, "IIF((_id > @p0), _id, @p1)", 10, 0);
        }

        [TestMethod]
        public void Linq_Cast_Convert_Types()
        {
            // cast only fromType Double/Decimal to Int32/64

            // int cast/convert/parse
            TestExpr<User>(x => (int)x.Salary, "INT32(Salary)");
            TestExpr<User>(x => (int)x.Salary, "INT32(Salary)");
            TestExpr<User>(x => (double)x.Id, "_id");
            TestExpr(x => Convert.ToInt32("123"), "INT32(@p0)", "123");
            TestExpr(x => Int32.Parse("123"), "INT32(@p0)", "123");
        }

        [TestMethod]
        public void Linq_Methods()
        {
            // string instance methods
            TestExpr<User>(x => x.Name.ToUpper(), "UPPER(Name)");
            TestExpr<User>(x => x.Name.Substring(10), "SUBSTRING(Name, @p0)", 10);
            TestExpr<User>(x => x.Name.Substring(10, 20), "SUBSTRING(Name, @p0, @p1)", 10, 20);
            TestExpr<User>(x => x.Name.Replace('+', '-'), "REPLACE(Name, @p0, @p1)", "+", "-");
            TestExpr<User>(x => x.Name.IndexOf("m"), "INDEXOF(Name, @p0)", "m");
            TestExpr<User>(x => x.Name.IndexOf("m", 20), "INDEXOF(Name, @p0, @p1)", "m", 20);
            TestExpr<User>(x => x.Name.ToUpper().Trim(), "TRIM(UPPER(Name))");
            TestExpr<User>(x => x.Name.ToUpper().Trim().PadLeft(5, '0'), "LPAD(TRIM(UPPER($.Name)), @p0, @p1)", 5, "0");

            // string LIKE
            TestExpr<User>(x => x.Name.StartsWith("Mauricio"), "Name LIKE (@p0 + '%')", "Mauricio");
            TestExpr<User>(x => x.Name.Contains("Bezerra"), "Name LIKE ('%' + @p0 + '%')", "Bezerra");
            TestExpr<User>(x => x.Name.EndsWith("David"), "Name LIKE ('%' + @p0)", "David");
            TestExpr<User>(x => x.Name.StartsWith(x.Address.Street), "Name LIKE (Address.Street + '%')");

            // string members
            TestExpr<User>(x => x.Address.Street.Length, "LENGTH(Address.Street)");
            TestExpr(x => string.Empty, "''");

            // string static methods
            TestExpr<User>(x => string.IsNullOrEmpty(x.Name), "(LENGTH(Name) = 0)");
            TestExpr<User>(x => string.IsNullOrWhiteSpace(x.Name), "(LENGTH(TRIM(Name)) = 0)");

            // guid initialize/converter
            TestExpr<User>(x => Guid.NewGuid(), "GUID()");
            TestExpr<User>(x => Guid.Empty, "GUID('00000000-0000-0000-0000-000000000000')");
            TestExpr<User>(x => Guid.Parse("1A3B944E-3632-467B-A53A-206305310BAC"), "GUID(@p0)", "1A3B944E-3632-467B-A53A-206305310BAC");

            // toString/Format
            TestExpr<User>(x => x.Id.ToString(), "STRING(_id)");
            TestExpr<User>(x => x.CreatedOn.ToString(), "STRING(CreatedOn)");
            TestExpr<User>(x => x.CreatedOn.ToString("yyyy-MM-dd"), "FORMAT(CreatedOn, @p0)", "yyyy-MM-dd");
            TestExpr<User>(x => x.Salary.ToString("#.##0,00"), "FORMAT(Salary, @p0)", "#.##0,00");

            // adding dates
            TestExpr<User>(x => x.CreatedOn.AddYears(5), "DATEADD('y', @p0, CreatedOn)", 5);
            TestExpr<User>(x => x.CreatedOn.AddMonths(5), "DATEADD('M', @p0, CreatedOn)", 5);
            TestExpr<User>(x => x.CreatedOn.AddDays(5), "DATEADD('d', @p0, CreatedOn)", 5);
            TestExpr<User>(x => x.CreatedOn.AddHours(5), "DATEADD('h', @p0, CreatedOn)", 5);
            TestExpr<User>(x => x.CreatedOn.AddMinutes(5), "DATEADD('m', @p0, CreatedOn)", 5);
            TestExpr<User>(x => x.CreatedOn.AddSeconds(5), "DATEADD('s', @p0, CreatedOn)", 5);

            // using dates properties
            TestExpr<User>(x => x.CreatedOn.Year, "YEAR(CreatedOn)");
            TestExpr<User>(x => x.CreatedOn.Month, "MONTH(CreatedOn)");
            TestExpr<User>(x => x.CreatedOn.Day, "DAY(CreatedOn)");
            TestExpr<User>(x => x.CreatedOn.Hour, "HOUR(CreatedOn)");
            TestExpr<User>(x => x.CreatedOn.Minute, "MINUTE(CreatedOn)");
            TestExpr<User>(x => x.CreatedOn.Second, "SECOND(CreatedOn)");

            TestExpr<User>(x => x.CreatedOn.Date, "DATETIME(YEAR(CreatedOn), MONTH(CreatedOn), DAY(CreatedOn))");

            // static date
            TestExpr<User>(x => DateTime.Now, "NOW()");
            TestExpr<User>(x => DateTime.UtcNow, "NOW_UTC()");
            TestExpr<User>(x => DateTime.Today, "TODAY()");
        }

        [TestMethod]
        public void Linq_Dictionary_Index_Access()
        {
            // index value will be evaluate when "get_Item" method call
            TestExpr<User>(x => x.Phones[0].Number, "Phones[0].Number");
            TestExpr<User>(x => x.MetaData["KeyLocal"], "$.MetaData.KeyLocal");

            TestExpr<User>(x => x.MetaData["Key Local"].Street, "$.MetaData.['Key Local'].Street");
        }

        [TestMethod]
        public void Linq_New_Instance()
        {
            // new class
            TestExpr<User>(x => new { x.Name, x.Address }, "{ Name, Address }");
            TestExpr<User>(x => new { N = x.Name, A = x.Address }, "{ N: $.Name, A: $.Address }");

            // new array
            TestExpr<User>(x => new int[] { x.Id, 6, 7 }, "[_id, @p0, @p1]", 6, 7);

            // new fixed types
            TestExpr(x => new DateTime(2018, 5, 28), "DATETIME(@p0, @p1, @p2)", 2018, 5, 28);

            TestExpr(x => new Guid("1A3B944E-3632-467B-A53A-206305310BAC"), "GUID(@p0)", "1A3B944E-3632-467B-A53A-206305310BAC");

            // new instances with initializers
            TestExpr<User>(x => new User { Id = 1, Active = false }, "{ _id: @p0, Active: @p1 }", 1, false);

            // used in UpdateMany extend document
            TestExpr<User>(x => new User { Name = x.Name.ToUpper(), Salary = x.Salary * 2 }, "{ Name: UPPER($.Name), Salary: ($.Salary * @p0) }", 2);


        }

        [TestMethod]
        public void Linq_Composite_Key()
        {
            // using composite key new class initializer
            TestExpr<User>(x => x.Address == new Address { Number = 555 }, "(Address = { Number: @p0 })", 555);

            // using 2 levels
            TestExpr<User>(x => x.Address == new Address { Number = 1, City = new City { Country = "BR", CityName = "POA" } },
                "(Address = { Number: @p0, City: { Country: @p1, CityName: @p2 } })", 1, "BR", "POA");
        }

        [TestMethod]
        public void Linq_Coalesce()
        {
            TestExpr<User>(x => x.DomainName ?? x.Name, "COALESCE(USER_DOMAIN_NAME, Name)");

            TestExpr<City>(x => (x.CityName ?? x.Country) == DateTime.Now.Year.ToString(), 
                "(COALESCE(CityName, Country) = STRING(YEAR(NOW())))");
        }

        [TestMethod]
        public void Linq_Complex_Expressions()
        {
            TestExpr<User>(x => new
            {
                CityName = x.Address.City.CityName,
                Count = x.Phones.Where(p => p.Type == PhoneType.Landline).Count(),
                List = x.Phones.Where(p => p.Number > x.Salary).Select(p => p.Number).ToArray()
            },
            @"
            {
                CityName: $.Address.City.CityName,
                Count: COUNT($.Phones[(@.Type = @p0)]),
                List: ARRAY(($.Phones[(@.Number > $.Salary)] => @.Number))
            }", 
            (int)PhoneType.Landline);
        }

        [TestMethod]
        public void Linq_BsonDocument_Navigation()
        {
            TestExpr<BsonValue>(x => x["name"].AsString, "$.name");
            TestExpr<BsonValue>(x => x["first"]["name"], "$.first.name");
            TestExpr<BsonValue>(x => x["arr"][0]["demo"], "$.arr[0].demo");
            TestExpr<BsonValue>(x => x["age"] == 1, "($.age = @p0)", 1);
        }

        [TestMethod]
        public void Linq_Custom_Field_Name()
        {
            // first use
            TestExpr<User>(x => x.DomainName, "$.USER_DOMAIN_NAME");

            // in creation new class
            TestExpr<User>(x => new { x.DomainName }, "{ DomainName: $.USER_DOMAIN_NAME }");
        }

        [TestMethod]
        public void Linq_Use_Enumerable_As_Root()
        {
            // when root parameter is IEnumerable, root symbol must be *
            TestExpr<IEnumerable<User>>(x => x.Count(), "COUNT(*)");
            TestExpr<IEnumerable<User>>(x => x.Sum(u => u.Id), "SUM(* => @._id)");
            
            //**TestExpr<IEnumerable<User>>(x => new
            //**{
            //**    year = x.Select(p => p.CreatedOn.Year).First(),
            //**    sum = x.Sum(u => u.Salary)
            //**}, "{ 'year': FIRST((@ => YEAR(@.CreatedOn))), 'sum': SUM(@ => @.Salary) }");
        }

        #region Test helper

        /// <summary>
        /// Compare LINQ expression with expected BsonExpression
        /// </summary>
        [DebuggerHidden]
        private BsonExpression Test<T, K>(Expression<Func<T, K>> expr, BsonExpression expect, params BsonValue[] args)
        {
            var expression = _mapper.GetExpression(expr);

            Assert.AreEqual(expect.Source, expression.Source);

            Assert.AreEqual(expression.Parameters.Keys.Count, args.Length, "Number of parameter are different than expected");

            var index = 0;

            foreach(var par in args)
            {
                var pval = expression.Parameters["p" + (index++).ToString()];

                Assert.AreEqual(par, pval, "Expression: " + expect.Source);
            }

            return expression;
        }

        [DebuggerHidden]
        private BsonExpression TestExpr(Expression<Func<object, object>> expr, BsonExpression expect, params BsonValue[] args)
        {
            return this.Test<object, object>(expr, expect, args);
        }

        [DebuggerHidden]
        private BsonExpression TestExpr<T>(Expression<Func<T, object>> expr, BsonExpression expect, params BsonValue[] args)
        {
            return this.Test<T, object>(expr, expect, args);
        }

        [DebuggerHidden]
        private BsonExpression TestPredicate<T>(Expression<Func<T, bool>> expr, BsonExpression expect, params BsonValue[] args)
        {
            return this.Test<T, bool>(expr, expect, args);
        }

        /// <summary>
        /// Execute test but expect an exception
        /// </summary>
        [DebuggerHidden]
        private void TestException<T, TExecption>(Func<Expression<Func<T, object>>> fn)
            where TExecption : Exception
        {
            try
            {
                var test = fn();

                this.TestExpr<T>(test, "$");

                Assert.Fail("Should throw exception");
            }
            catch(TExecption)
            {
            }
        }

        #endregion
    }
}