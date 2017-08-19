using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Compile and execute simple expressions using BsonDocuments. Used in indexes and updates operations
    /// </summary>
    internal partial class LiteExpression
    {
        private Func<BsonDocument, IEnumerable<BsonValue>> _expr;

        public string Expr { get; private set; }

        public LiteExpression(string expression)
        {
            this.Expr = expression;

            _expr = Compile(expression);
        }

        /// <summary>
        /// Execute expression and returns IEnumerable values (can returns NULL if no elements).
        /// </summary>
        public IEnumerable<BsonValue> Execute(BsonDocument doc, bool includeNullIfEmpty = true)
        {
            var index = 0;
            var values = _expr(doc);

            foreach (var value in values)
            {
                index++;
                yield return value;
            }

            if (index == 0 && includeNullIfEmpty) yield return BsonValue.Null;
        }

        private static Dictionary<string, Func<BsonDocument, IEnumerable<BsonValue>>> _cache = new Dictionary<string, Func<BsonDocument, IEnumerable<BsonValue>>>();

        /// <summary>
        /// Compile a string expression into a Linq function to run compiled in memory
        /// </summary>
        public static Func<BsonDocument, IEnumerable<BsonValue>> Compile(string expression)
        {
            Func<BsonDocument, IEnumerable<BsonValue>> fn;

            if (_cache.TryGetValue(expression, out fn)) return fn;

            lock(_cache)
            {
                if (_cache.TryGetValue(expression, out fn)) return fn;

                var s = new StringScanner(expression);
                var doc = Expression.Parameter(typeof(BsonDocument), "doc");
                var expr = ParseExpression(s, doc);

                var lambda = Expression.Lambda<Func<BsonDocument, IEnumerable<BsonValue>>>(expr, doc);

                fn = lambda.Compile();
                _cache[expression] = fn;

                return fn;
            }
        }

        /// <summary>
        /// Start parse string into linq expression. Read path, function or base type bson values (int, double, bool, string)
        /// </summary>
        public static Expression ParseExpression(StringScanner s, ParameterExpression doc)
        {
            if(s.Match(@"\$")) // read path
            {
                s.Scan(@"\$\.?"); // read root
                var root = typeof(LiteExpression).GetMethod("Root");
                var name = Expression.Constant(s.Scan(@"[\$\-\w]+"));
                var expr = Expression.Call(root, doc, name) as Expression;

                // parse the rest of path
                while (!s.HasTerminated)
                {
                    var result = ParsePath(s, expr);

                    if(result == null) break;

                    expr = result;
                }

                return expr;
            }
            else if (s.Match(@"-?\d*\.\d+")) // read double
            {
                var number = Convert.ToDouble(s.Scan(@"-?\d*\.\d+"));
                var value = Expression.Constant(new BsonValue(number));

                return Expression.NewArrayInit(typeof(BsonValue), value);
            }
            else if (s.Match(@"-?\d+")) // read int
            {
                var number = Convert.ToInt32(s.Scan(@"-?\d+"));
                var value = Expression.Constant(new BsonValue(number));

                return Expression.NewArrayInit(typeof(BsonValue), value);
            }
            else if (s.Match(@"(true|false)")) // read bool
            {
                var boolean = Convert.ToBoolean(s.Scan(@"(true|false)"));
                var value = Expression.Constant(new BsonValue(boolean));

                return Expression.NewArrayInit(typeof(BsonValue), value);
            }
            else if (s.Match(@"'")) // read string
            {
                var str = s.Scan(@"'([\s\S]*)?'", 1);
                var value = Expression.Constant(new BsonValue(str));

                return Expression.NewArrayInit(typeof(BsonValue), value);
            }
            else if (s.Match(@"\w+")) // read function
            {
                // get static method from this class
                var method = typeof(LiteExpression).GetMethod(s.Scan(@"\w+").ToUpper());
                var parameters = new List<Expression>();

                s.Scan(@"\s*\(");
              
                while(!s.HasTerminated && s.Scan(@"\s*\)\s*").Length == 0)
                {
                    var parameter = ParseExpression(s, doc);

                    parameters.Add(parameter);

                    s.Scan(@"\s*,\s*");
                }

                return Expression.Call(method, parameters.ToArray());
            }

            throw new InvalidOperationException("Invalid bson expression: " + s.ToString());
        }

        /// <summary>
        /// Implement a JSON-Path like navigation on BsonDocument. Support a simple range of paths
        /// $ => Root document
        /// $.Name => Name value from root
        /// $.Address.Street => Street value from Address sub document
        /// $.Items => Array value from Items
        /// $.Items[*] = All values (IEnumerable) from Items array
        /// $.Items[0] = First value from Items array
        /// $.Items[-1] = Last value from Items array
        /// $.Items[*].Age = All age values from all items array
        /// </summary>
        private static Expression ParsePath(StringScanner s, Expression expr)
        {
            if (s.Match(@"\.[\$\-\w]+"))
            {
                var member = typeof(LiteExpression).GetMethod("Member");
                var name = Expression.Constant(s.Scan(@"\.([\$\-\w]+)", 1));
                return Expression.Call(member, expr, name);
            }
            else if (s.Match(@"\["))
            {
                var array = typeof(LiteExpression).GetMethod("Array");
                var i = s.Scan(@"\[(-?[\d+\*])\]", 1);
                var index = i == "*" ? int.MaxValue : Convert.ToInt32(i);

                return Expression.Call(array, expr, Expression.Constant(index));
            }
            else
            {
                return null;
            }
        }

        #region Path methods

        /// <summary>
        /// Returns value from root document. Returns same document if name are empty
        /// </summary>
        public static IEnumerable<BsonValue> Root(BsonDocument value, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                yield return value;
            }
            else
            {
                BsonValue item;

                if (value.TryGetValue(name, out item))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Returns all values from array according index. If index are MaxValue, return all values
        /// </summary>
        public static IEnumerable<BsonValue> Array(IEnumerable<BsonValue> values, int index = int.MaxValue)
        {
            foreach (var value in values)
            {
                if (value.IsArray)
                {
                    var arr = value.AsArray;

                    if (index == int.MaxValue)
                    {
                        foreach (var item in arr) yield return item;
                    }
                    else
                    {
                        var idx = index < 0 ? arr.Count + index : index;

                        if (arr.Count > idx)
                        {
                            yield return arr[idx];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Return a value from a value as document. If value are not a document, do not return anything
        /// </summary>
        public static IEnumerable<BsonValue> Member(IEnumerable<BsonValue> values, string name)
        {
            foreach (var value in values)
            {
                if (value.IsDocument)
                {
                    BsonValue item;

                    if (value.AsDocument.TryGetValue(name, out item))
                    {
                        yield return item;
                    }
                }
            }
        }

        #endregion

        public override string ToString()
        {
            return this.Expr;
        }
    }
}
