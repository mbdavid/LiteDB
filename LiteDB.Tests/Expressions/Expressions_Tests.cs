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

namespace LiteDB.Tests.Expressions
{
    [TestClass]
    public class Expressions_Tests
    {
        [TestMethod]
        public void Expressions_Constants()
        {
            BsonValue K(string s) { return BsonExpression.Create(s).ExecuteScalar(); };

            K(@"123").ExpectValue(123);
            K(@"null").ExpectValue(BsonValue.Null);
            K(@"15.9").ExpectValue(15.9);
            K(@"true").ExpectValue(true);
            K(@"false").ExpectValue(false);
            K(@"'my string'").ExpectValue("my string");
            K(@"""my string""").ExpectValue("my string");
            K(@"[1,2]").ExpectArray(1, 2);
            K(@"[]").ExpectJson("[]");
            K(@"{a:1}").ExpectJson("{a:1}");
            K(@"{a:true,i:0}").ExpectJson("{a:true,i:0}");
        }

        [TestMethod]
        public void Expression_Fields()
        {
            IEnumerable<string> F(string s) { return BsonExpression.Create(s).Fields; };

            // simple case
            F("$.Name").ExpectValues("Name");

            F("JustName").ExpectValues("JustName");
            F("$.[\"My First Name\"]").ExpectValues("My First Name");

            // only root field
            F("$.Name.First").ExpectValues("Name");
            F("$.Items[*].Type").ExpectValues("Items");

            // inside new document/array
            F("{ Active, _id }").ExpectValues("Active", "_id");
            F("{ Active, _id: 1 }").ExpectValues("Active");
            F("[ Active, _id, null, UPPER(Name.First)]").ExpectValues("Active", "_id", "Name");

            // no fields
            F("{ Active: 1, _id: 2 }").ExpectCount(0);
            F("123").ExpectCount(0);
            F("UPPER(@p0) = 'JOHN' OR YEAR(NOW()) = 2018").ExpectCount(0);

            // duplicate 
            F("{ Active: active, NewActive: active, Root: $ }").ExpectValues("active", "$");

            // case insensitive (only first field is return)
            F("{ Active: active, NewActive: ACTIVE }").ExpectValues("active");

            // with no root in array
            F("Items[0].Type = Age").ExpectValues("Items", "Age");

            // with root and MAP :: ($.Items[$.Root = 1] => @.Type = @.Age)
            F("Items[$.Root = 1].Type = Age").ExpectValues("Items", "Root");

            // with root and MAP :: ($.Items[$.Root = 1] => @.Type = $.Age)
            F("Items[$.Root = 1].Type = $.Age").ExpectValues("Items", "Root", "Age");

            // predicate + method
            F("_id = Age + YEAR(DATETIME(2000, 1, DAY(NewField))) AND UPPER(TRIM(Name)) = @0")
                .ExpectValues("_id", "Age", "NewField", "Name");
        }

        [TestMethod]
        public void Expression_Immutable()
        {
            bool I(string s) { return BsonExpression.Create(s).IsImmutable; };

            // some immutable expression
            I("_id").ExpectValue(true);
            I("{ a: 1, n: UPPER(name) }").ExpectValue(true);
            I("GUID('00000000-0000-0000-0000-000000000000')").ExpectValue(true);

            // using method that are not immutable 
            I("_id + DAY(NOW())").ExpectValue(false);
            I("r + 10 > 10 AND GUID() = true").ExpectValue(false);
            I("r + 10 > 10 AND Name LIKE OBJECTID() + '%'").ExpectValue(false);
            I("_id > @0").ExpectValue(false);
        }
    }
}