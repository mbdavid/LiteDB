using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests.Document
{
    [TestClass]
    public class Expression_Tests
    {
        private static string jdoc = @"
        {
            _id: 1, $ref: 1.1
            a: 2,
            b: [3, 4, 5],
            c: null,
            d: [6, { e: 7, f: [8, 9] } ],
            f: { g: 10 },
            h: ""litedb"",
            ""i!"": 11,
            ""j!"": { ""k!"": 12, ""l!"" : [13,14] }
        }";


        [TestMethod]
        public void Json_Paths()
        {
            // all test must return a value (if no value to return, returns null)

            // Test("$", JsonSerializer.Serialize(JsonSerializer.Deserialize(jdoc))); // full document (as empty)
            // Test("_id", "1").AssertEquals("$._id"); // direct access
            // Test("_id2", "<null>"); // missing field
            // Test("a", "2").AssertEquals("$.a"); // direct field access
            // Test("$.['i!']", "11").AssertEquals("$.[\"i!\"]"); // access field using [ ] - must use $ before
            // Test("$.['j!'].[\"k!\"]", "12").AssertEquals("$.[\"j!\"].[\"k!\"]"); // access field using [ ] - must use $ before
            // Test("$.['j!'].['l!'][ * ]", "13;14").AssertEquals("$.[\"j!\"].[\"l!\"][*]"); ; // access field using [ ] - must use $ before
            // Test("b", "[3,4,5]"); // direct access returns array
            // Test("b[0]", "3"); // fixed array position
            // Test("b[*]", "3;4;5"); // all array items (IEnumerable)
            // Test("c", "<null>"); // null value
            // Test("d[1].f[0]", "8"); // inner array item
            // Test("d[1].f[-1]", "9"); // last item (-1)
            // Test("d[1].f[777]", "<null>"); // missing array index
            // Test("d[1].f[*]", "8;9"); // all items inside inner array
            // Test("f", "{\"g\":10}"); // direct access return document
            // Test("f.g", "10"); // inner document field access
            // Test("f.g.hhh", "<null>"); // inner document missing field


            Test("b[@>=4]", "4;5"); // array condition
            Test("b[@ % 2 = 1]", "3;5"); // more complex array condition

        }

        [TestMethod]
        public void Json_Expressions()
        {
            /*
            Assert.AreEqual("2", Exec("1 + 1")); // simple arithmetic 
            Assert.AreEqual("7", Exec("1 + (2 * 3)")); // using order
            Assert.AreEqual("7", Exec("$._id + (2 * 3)")); // adding document field
            Assert.AreEqual("", Exec("1 + null")); // arithmetic with null
            Assert.AreEqual("", Exec("1 + true")); // arithmetic with with bool
            Assert.AreEqual("1two", Exec("1 + 'two'")); // arithmetic with with string (string concat)

            // all operations/method support IEnumerable as input and output
            Assert.AreEqual("6;8;10", Exec("$.b[*] * 2")); // return multiple values
            Assert.AreEqual("6;8;10", Exec("$.b[*] + $.b[*]")); // arithmetic with IEnumerable both sides
            Assert.AreEqual("7;9;10", Exec("$.b[*] + $.b[@ >= 4]")); // arithmetic with IEnumerable different index size


            Assert.AreEqual("DEMO", Exec("UPPER('demo')")); // using methods
            Assert.AreEqual("12", Exec("SUM($.b[*])")); // group methods (works with IEnumerable input)
            Assert.AreEqual("[3,5]", Exec("ARRAY($.b[@ % 2 = 1])")); // convert IEnumerable result into an array

            // string methods
            Assert.AreEqual("LITEDB", Exec("UPPER($.h)"));
            Assert.AreEqual("001", Exec("LPAD($._id, 3, '0')"));
            Assert.AreEqual("liteDB", Exec("SUBSTRING($.h, 0, 4) + UPPER(SUBSTRING($.h, 4))"));
            Assert.AreEqual("3~4~5", Exec("JOIN($.b[*], '~')")); // join using IEnumerable

            // more methods
            Assert.AreEqual("_id;a;b;c;d;f;h", Exec("KEYS($)")); // get keys from document
            Assert.AreEqual("lower;lower;gt5", Exec("IIF($.b[*] >= 5, 'gt' + $.b[*], 'lower')")); // conditional inside IEnumerable
            */

        }

        [TestMethod]
        public void Update_Document_Using_Set_Expressions()
        {
            var doc = JsonSerializer.Deserialize(jdoc).AsDocument;

            doc.Set("_id", 101);
            doc.Set("$.a", 102);
            doc.Set("c", new BsonExpression("$._id + 9"));
            doc.Set("$.h", new BsonExpression("UPPER($.h)"));

            doc.Set("$.d[1].f", 103, true);

            Assert.AreEqual(101, doc["_id"].AsInt32);
            Assert.AreEqual(102, doc["a"].AsInt32);
            Assert.AreEqual(110, doc["c"].AsInt32);
            Assert.AreEqual("LITEDB", doc["h"].AsString);
            Assert.AreEqual(103, doc["d"].AsArray[1].AsDocument["f"].AsArray[2].AsInt32);

            // _id was changed
            Assert.IsTrue(doc.Set("_id", 5));

            // nothing was changed
            Assert.IsFalse(doc.Set("_id", new BsonExpression("$._id + 1 - 1")));
        }

        /// <summary>
        /// Run expression and returns formatted expression
        /// </summary>
        private static string Test(string expr, string expected)
        {
            var e = new BsonExpression(expr);
            var doc = JsonSerializer.Deserialize(jdoc);
            var result = e.Execute(doc.AsDocument);

            var str = string.Join(";", result.Select(x => x.IsArray || x.IsDocument ? JsonSerializer.Serialize(x) : x.IsNull ? "<null>" : x.AsString));

            Assert.AreEqual(expected, str);

            var source = e.ToString();

            return source;
        }
    }

    public static class StringExtensionAssert
    {
        public static void AssertEquals(this string str, string expect)
        {
            Assert.AreEqual(expect, str);
        }
    }
}