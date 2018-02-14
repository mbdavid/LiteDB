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

namespace LiteDB.Tests.Document
{
    [TestClass]
    public class BsonExpression_Tests
    {
        [TestMethod]
        public void BsonExpression_Path() => this.RunTest("Path.txt");

        [TestMethod]
        public void BsonExpression_Format() => this.RunTest("Format.txt");

        [TestMethod]
        public void BsonExpression_Operator() => this.RunTest("Operator.txt");

        [TestMethod]
        public void BsonExpression_Method() => this.RunTest("Method.txt");

        public void RunTest(string filename)
        {
            var tests = this.ReadTests("LiteDB.Tests.Document.ExprTests." + filename);

            foreach(var test in tests)
            {
                var expr = BsonExpression.Create(test.Expression);
                
                // test formatted source
                if (test.Formatted != null)
                {
                    Assert.AreEqual(test.Formatted, expr.Source, "Invalid formatted in " + test.Expression + " (" + filename + ")");
                }

                if (test.Results.Count == 0) continue;

                // test result
                var doc = JsonSerializer.Deserialize(test.JsonDocument ?? "{}") as BsonDocument;

                var result = expr.Execute(doc, true).ToList();

                if (!result.SequenceEqual(test.Results))
                {
                    Assert.AreEqual(string.Join("; ", result.Select(x => x.ToString())),
                        string.Join("; ", test.Results.Select(x => x.ToString())),
                        test.Comment + " : " + test.Expression + " (" + filename + ")");
                }
            }
        }

        public List<ExprTest> ReadTests(string name)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            var tests = new List<ExprTest>();

            using (var reader = new StreamReader(stream))
            {
                var s = new StringScanner(reader.ReadToEnd().Trim() + "\n");

                string json = null;
                ExprTest test = null;
                var comment = "";

                while (!s.HasTerminated)
                {
                    if (s.Match(@">")) // new test
                    {
                        test = new ExprTest { JsonDocument = json, Expression = s.Scan(@">\s*([\s\S]*?)\n", 1).Trim(), Comment = comment };
                        tests.Add(test);
                    }
                    else if (s.Match("=")) // new result
                    {
                        test.Results.Add(JsonSerializer.Deserialize(s.Scan(@"=\s*([\s\S]*?)\n", 1).Trim()));
                    }
                    else if (s.Match("~")) // expected format
                    {
                        test.Formatted = s.Scan(@"~\s*([\s\S]*?)\n", 1).Trim();
                    }
                    else if (s.Match(@"\#")) // comment in test
                    {
                        comment = s.Scan(@"\#\s*([\s\S]*?)\n", 1).Trim();
                    }
                    else if (s.Match(@"\{")) // new doc
                    {
                        var pos = s.Index;
                        JsonSerializer.Deserialize(s);
                        json = s.Source.Substring(pos, s.Index - pos);
                    }
                    else
                    {
                        s.Scan(@"[\s\S]*?\n"); // read line
                    }
                }
            }

            return tests;
        }

    }

    public class ExprTest
    {
        public string JsonDocument { get; set; }
        public string Expression { get; set; }
        public string Formatted { get; set; }
        public List<BsonValue> Results { get; set; } = new List<BsonValue>();
        public string Comment { get; set; }
    }
}