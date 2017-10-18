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
            _id: 1,
            a: 2,
            b: [3, 4, 5],
            c: null,
            d: [6, { e: 7, f: [8, 9] } ],
            f: { g: 10 },
            h: ""litedb""
        }";


        [TestMethod]
        public void Json_Paths()
        {
            Assert.AreEqual("1", Exec("$._id")); // direct access
            Assert.AreEqual("", Exec("$._id2")); // missing field
            Assert.AreEqual("2", Exec("$.a")); // direct field access
            Assert.AreEqual("[3,4,5]", Exec("$.b")); // direct access returns array
            Assert.AreEqual("3", Exec("$.b[0]")); // fixed array position
            Assert.AreEqual("3;4;5", Exec("$.b[*]")); // all array items (IEnumerable)
            Assert.AreEqual("4;5", Exec("$.b[@>=4]")); // array condition
            Assert.AreEqual("3;5", Exec("$.b[@ % 2 = 1]")); // more complex array condition
            Assert.AreEqual("", Exec("$.c")); // null value
            Assert.AreEqual("8", Exec("$.d[1].f[0]")); // inner array item
            Assert.AreEqual("9", Exec("$.d[1].f[-1]")); // last item (-1)
            Assert.AreEqual("", Exec("$.d[1].f[777]")); // missing array index
            Assert.AreEqual("8;9", Exec("$.d[1].f[*]")); // all items inside inner array
            Assert.AreEqual("{\"g\":10}", Exec("$.f")); // direct access return document
            Assert.AreEqual("10", Exec("$.f.g")); // inner document field access
            Assert.AreEqual("", Exec("$.f.g.hhh")); // inner document missing field
        }

        [TestMethod]
        public void Json_Expressions()
        {
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

        private static string Exec(string expr)
        {
            var e = new BsonExpression(expr);
            var doc = JsonSerializer.Deserialize(jdoc);
            var result = e.Execute(doc.AsDocument);

            return string.Join(";", result.Select(x => x.IsArray || x.IsDocument ? JsonSerializer.Serialize(x) : x.AsString));
        }
    }
}