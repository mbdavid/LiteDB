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
    /// <summary>
    /// BsonExpression unit test from external text file 
    /// File format:
    /// "#" new comment used in next tests
    /// "{" new document to be used in next tests (support many)
    /// ">" indicate new expression to test (execute over each input document)
    /// ">>" indicate new expression to test in aggregate mode (execute over all input document)
    /// "~" expect expression formatted (Source)
    /// "-" expect used fields (comma separated) (Fields)
    /// "@" expect parameter value  
    /// "=" expect result (support multilines per test)
    /// </summary>

    [TestClass]
    public class BsonExpression_Tests
    {
        [TestMethod]
        public void BsonExpression_Aggregate() => this.RunTest("Aggregate.txt");

        [TestMethod]
        public void BsonExpression_Path() => this.RunTest("Path.txt");

        [TestMethod]
        public void BsonExpression_Format() => this.RunTest("Format.txt");

        [TestMethod]
        public void BsonExpression_Operator() => this.RunTest("Operator.txt");

        [TestMethod]
        public void BsonExpression_Method() => this.RunTest("Method.txt");

        [TestMethod]
        public void BsonExpression_Parameter() => this.RunTest("Parameter.txt");

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

                // test fields
                if (test.Fields != null)
                {
                    var areEquivalent = 
                        (expr.Fields.Count == test.Fields.Length) && 
                        !expr.Fields.Except(test.Fields).Any();

                    Assert.IsTrue(areEquivalent, "Invalid Fields");
                }

                if (test.Results.Count == 0) continue;

                expr.Parameters.Clear();
                test.Parameters.CopyTo(expr.Parameters);

                // test result
                var inputs = new List<BsonDocument>();

                if (test.Documents.Count == 0)
                {
                    inputs.Add(new BsonDocument());
                }
                else
                {
                    foreach (var d in test.Documents)
                    {
                        inputs.Add(JsonSerializer.Deserialize(d) as BsonDocument);
                    }
                }

                var results = new List<BsonValue>();

                if (test.Aggregate)
                {
                    results.Add(expr.Execute(inputs, true).First());
                }
                else
                {
                    foreach(var doc in inputs)
                    {
                        var r = expr.Execute(doc, true);

                        results.AddRange(r);
                    }
                }

                var jsonResult = JsonSerializer.Serialize(new BsonArray(results));
                var jsonExpect = JsonSerializer.Serialize(new BsonArray(test.Results));

                if (jsonResult != jsonExpect)
                {
                    Assert.AreEqual(string.Join("; ", test.Results.Select(x => x.ToString())),
                        string.Join("; ", results.Select(x => x.ToString())),
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

                var docs = new List<string>();
                var clearDocs = true;
                ExprTest test = null;
                var comment = "";

                while (!s.HasTerminated)
                {
                    if (s.Match(@">")) // new test
                    {
                        test = new ExprTest
                        {
                            Aggregate = s.Scan(">>").Length > 0,
                            Documents = new List<string>(docs),
                            Expression = s.Scan(@">?\s*([\s\S]*?)\n", 1).Trim(),
                            Comment = comment
                        };
                        tests.Add(test);
                        clearDocs = true;
                    }
                    else if (s.Match("=")) // new result
                    {
                        test.Results.Add(JsonSerializer.Deserialize(s.Scan(@"=\s*([\s\S]*?)\n", 1).Trim()));
                    }
                    else if (s.Match("~")) // expected format
                    {
                        test.Formatted = s.Scan(@"~\s*([\s\S]*?)\n", 1).Trim();
                    }
                    else if (s.Match("-")) // expected fields
                    {
                        test.Fields = s.Scan(@"-\s*([\s\S]*?)\n", 1).Trim().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                    }
                    else if (s.Match(@"\@\w+")) // expected parameter
                    {
                        test.Parameters[s.Scan(@"\@(\w+)\s+", 1)] = JsonSerializer.Deserialize(s.Scan(@"([\s\S]*?)\n", 1).Trim());
                    }
                    else if (s.Match(@"\#")) // comment in test
                    {
                        comment = s.Scan(@"\#\s*([\s\S]*?)\n", 1).Trim();
                    }
                    else if (s.Match(@"\{")) // new doc
                    {
                        if (clearDocs) docs.Clear();

                        var pos = s.Index;
                        JsonSerializer.Deserialize(s);
                        var d = s.Source.Substring(pos, s.Index - pos);
                        docs.Add(d);

                        clearDocs = false;
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
        public List<string> Documents { get; set; }
        public string Expression { get; set; }
        public string Formatted { get; set; }
        public bool Aggregate { get; set; }
        public string[] Fields { get; set; }
        public List<BsonValue> Results { get; set; } = new List<BsonValue>();
        public BsonDocument Parameters { get; set; } = new BsonDocument();
        public string Comment { get; set; }
    }
}