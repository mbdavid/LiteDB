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
    public class Expressions_Tests
    {
        [TestMethod]
        public void Expressions_Aggregate() => this.RunTest("Aggregate.txt");

        [TestMethod]
        public void Expressions_Path() => this.RunTest("Path.txt");

        [TestMethod]
        public void Expressions_Format() => this.RunTest("Format.txt");

        [TestMethod]
        public void Expressions_Operator() => this.RunTest("Operator.txt");

        [TestMethod]
        public void Expressions_Method() => this.RunTest("Method.txt");

        [TestMethod]
        public void Expressions_Parameter() => this.RunTest("Parameter.txt");

        public static IEnumerable<BsonValue> PLUS_ONE(IEnumerable<BsonValue> values)
        {
            return values.Select(x => new BsonValue(x.AsInt32 + 1));
        }

        public void RunTest(string filename)
        {
            var tests = this.ReadTests("LiteDB.Tests.Expressions.ExprTests." + filename);

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
                    results.Add(expr.Execute(inputs).First());
                }
                else
                {
                    foreach(var doc in inputs)
                    {
                        var r = expr.Execute(doc);

                        results.AddRange(r);
                    }
                }

                var jsonResult = JsonSerializer.Serialize(new BsonArray(results));
                var jsonExpect = JsonSerializer.Serialize(new BsonArray(test.Results));

                if (jsonResult != jsonExpect)
                {
                    Assert.AreEqual(string.Join("; ", test.Results.Select(x => x?.ToString() ?? "<null>")),
                        string.Join("; ", results.Select(x => x?.ToString() ?? "<null>")),
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
                var lines = reader.ReadToEnd().Trim().Split('\n').Select(x => x.Trim()).ToArray();

                var docs = new List<string>();
                var clearDocs = true;
                ExprTest test = null;
                var comment = "";

                foreach(var line in lines)
                {
                    if (line.StartsWith(@">")) // new test
                    {
                        test = new ExprTest
                        {
                            Aggregate = line.StartsWith(">>"),
                            Documents = new List<string>(docs),
                            Expression = line.Substring(line.IndexOf(' ')).Trim(),
                            Comment = comment
                        };
                        tests.Add(test);
                        clearDocs = true;
                    }
                    else if (line.StartsWith("=")) // new result
                    {
                        var res = line.Substring(1).Trim();
                        test.Results.Add(JsonSerializer.Deserialize(res));
                    }
                    else if (line.StartsWith("~")) // expected format
                    {
                        test.Formatted = line.Substring(1).Trim();
                    }
                    else if (line.StartsWith("-")) // expected fields
                    {
                        var f = line.Substring(1).Trim();

                        test.Fields = f.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                    }
                    else if (line.StartsWith("@")) // expected parameter
                    {
                        var pname = line.Substring(1, line.IndexOf(' ')).Trim();
                        var pvalue = line.Substring(line.IndexOf(' ') + 1).Trim();

                        test.Parameters[pname] = JsonSerializer.Deserialize(pvalue);
                    }
                    else if (line.StartsWith("#")) // comment in test
                    {
                        comment = line.Substring(1).Trim();
                    }
                    else if (line.StartsWith(@"{")) // new doc
                    {
                        if (clearDocs) docs.Clear();

                        docs.Add(line);

                        clearDocs = false;
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