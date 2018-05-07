using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// Compile and execute simple expressions using BsonDocuments. Used in indexes and updates operations. See https://github.com/mbdavid/LiteDB/wiki/Expressions
    /// </summary>
    public partial class BsonExpression
    {
        #region Operator Functions Cache

        /// <summary>
        /// Operation definition by methods
        /// </summary>
        private static Dictionary<string, MethodInfo> _operators = new Dictionary<string, MethodInfo>
        {
            ["%"] = typeof(ExpressionOperators).GetMethod("MOD"),
            ["/"] = typeof(ExpressionOperators).GetMethod("DIVIDE"),
            ["*"] = typeof(ExpressionOperators).GetMethod("MULTIPLY"),
            ["+"] = typeof(ExpressionOperators).GetMethod("ADD"),
            ["-"] = typeof(ExpressionOperators).GetMethod("MINUS"),
            [">"] = typeof(ExpressionOperators).GetMethod("GT"),
            [">="] = typeof(ExpressionOperators).GetMethod("GTE"),
            ["<"] = typeof(ExpressionOperators).GetMethod("LT"),
            ["<="] = typeof(ExpressionOperators).GetMethod("LTE"),
            ["="] = typeof(ExpressionOperators).GetMethod("EQ"),
            ["!="] = typeof(ExpressionOperators).GetMethod("NEQ"),
            ["&&"] = typeof(ExpressionOperators).GetMethod("AND"),
            ["||"] = typeof(ExpressionOperators).GetMethod("OR")
        };

        /// <summary>
        /// List of all methods avaiable in expressions
        /// </summary>
        private static MethodInfo[] _methods = typeof(BsonExpression).GetMethods(BindingFlags.Public | BindingFlags.Static);

        #endregion

        #region Ctor

        private Func<BsonDocument, BsonValue, IEnumerable<BsonValue>> _func;

        public string Source { get; private set; }

        public BsonExpression(string expression)
        {
            if (expression != null)
            {
                this.Source = expression;
                _func = Compile(expression);
            }
        }

        internal BsonExpression(StringScanner s, bool pathOnly, bool arithmeticOnly)
        {
            var start = s.Index;
            _func = Compile(s, pathOnly, arithmeticOnly);
            this.Source = s.Source.Substring(start, s.Index - start);

            // add this expression to cache if not exists
            if (!_cache.ContainsKey(this.Source))
            {
                lock(_cache)
                {
                    _cache[this.Source] = _func;
                }
            }
        }

        #endregion

        #region Execution

        /// <summary>
        /// Execute expression and returns IEnumerable values (can returns NULL if no elements).
        /// </summary>
        public IEnumerable<BsonValue> Execute(BsonDocument doc, bool includeNullIfEmpty = true)
        {
            return this.Execute(doc, doc, includeNullIfEmpty);
        }

        /// <summary>
        /// Execute expression and returns IEnumerable values (can returns NULL if no elements).
        /// </summary>
        public IEnumerable<BsonValue> Execute(BsonDocument root, BsonValue current, bool includeNullIfEmpty = true)
        {
            if (this.Source == null) throw new ArgumentNullException("ctor(expression)");

            var index = 0;
            var values = _func(root, current ?? root);

            foreach (var value in values)
            {
                index++;
                yield return value;
            }

            if (index == 0 && includeNullIfEmpty) yield return BsonValue.Null;
        }

        #endregion

        #region Compiler

        private static Dictionary<string, Func<BsonDocument, BsonValue, IEnumerable<BsonValue>>> _cache = new Dictionary<string, Func<BsonDocument, BsonValue, IEnumerable<BsonValue>>>();

        /// <summary>
        /// Parse and compile an expression from a string. Do cache of expressions
        /// </summary>
        public static Func<BsonDocument, BsonValue, IEnumerable<BsonValue>> Compile(string expression)
        {
            Func<BsonDocument, BsonValue, IEnumerable<BsonValue>> fn;

            if (_cache.TryGetValue(expression, out fn)) return fn;

            lock (_cache)
            {
                if (_cache.TryGetValue(expression, out fn)) return fn;

                fn = Compile(new StringScanner(expression), false, true);

                _cache[expression] = fn;

                return fn;
            }
        }

        /// <summary>
        /// Parse and compile an expression from a stringscanner. Must define if will read a path only or support for full expression. Can parse only arithmetic (+/-/*/..) or full logic operators (=/!=/>/...)
        /// </summary>
        private static Func<BsonDocument, BsonValue, IEnumerable<BsonValue>> Compile(StringScanner s, bool pathOnly, bool arithmeticOnly)
        {
            var isRoot = pathOnly;
            var root = Expression.Parameter(typeof(BsonDocument), "root");
            var current = Expression.Parameter(typeof(BsonValue), "current");
            Expression expr;

            if (pathOnly)
            {
                // if read path, read first expression only
                // support missing $ as root
                s.Scan(@"\$\.?");
                expr = ParseSingleExpression(s, root, current, true);
            }
            else
            {
                // read all expression (a + b)
                // if include operator, support = > < && || too
                expr = ParseExpression(s, root, current, arithmeticOnly);
            }

            var lambda = Expression.Lambda<Func<BsonDocument, BsonValue, IEnumerable<BsonValue>>>(expr, root, current);

            return lambda.Compile();
        }

        #endregion

        #region Expression Parser

        private static Regex _reArithmetic = new Regex(@"^\s*(\+|\-|\*|\/|%)\s*");
        private static Regex _reOperator = new Regex(@"^\s*(\+|\-|\*|\/|%|=|!=|>=|>|<=|<|&&|\|\|)\s*");

        /// <summary>
        /// Start parse string into linq expression. Read path, function or base type bson values (int, double, bool, string)
        /// </summary>
        internal static Expression ParseExpression(StringScanner s, ParameterExpression root, ParameterExpression current, bool arithmeticOnly)
        {
            var first = ParseSingleExpression(s, root, current, false);
            var values = new List<Expression> { first };
            var ops = new List<string>();

            // read all blocks and operation first
            while (!s.HasTerminated)
            {
                // checks if must support arithmetic only (+, -, *, /)
                var op = s.Scan(arithmeticOnly ? _reArithmetic : _reOperator, 1);

                if (op.Length == 0) break;

                var expr = ParseSingleExpression(s, root, current, false);

                values.Add(expr);
                ops.Add(op);
            }

            var order = 0;

            // now, process operator in correct order
            while(values.Count >= 2)
            {
                var op = _operators.ElementAt(order);
                var n = ops.IndexOf(op.Key);

                if (n == -1)
                {
                    order++;
                }
                else
                {
                    // get left/right values to execute operator
                    var left = values.ElementAt(n);
                    var right = values.ElementAt(n + 1);

                    // process result in a single value
                    var result = Expression.Call(op.Value, left, right);

                    // remove left+right and insert result
                    values.Insert(n, result);
                    values.RemoveRange(n + 1, 2);

                    // remove operation
                    ops.RemoveAt(n);
                }
            }

            return values.Single();
        }

        /// <summary>
        /// Start parse string into linq expression. Read path, function or base type bson values (int, double, bool, string)
        /// </summary>
        internal static Expression ParseSingleExpression(StringScanner s, ParameterExpression root, ParameterExpression current, bool isRoot)
        {
            if (s.Match(@"[\$@]") || isRoot) // read root path
            {
                var r = s.Scan(@"[\$@]"); // read root/current
                var method = typeof(BsonExpression).GetMethod("Root");
                var name = Expression.Constant(s.Scan(@"\.?([\$\-\w]+)", 1));
                var expr = Expression.Call(method, r == "@" ? current : root, name) as Expression;

                // parse the rest of path
                while (!s.HasTerminated)
                {
                    var result = ParsePath(s, expr, root);

                    if (result == null) break;

                    expr = result;
                }

                return expr;
            }
            else if (s.Match(@"-?\d*\.\d+")) // read double
            {
                var number = Convert.ToDouble(s.Scan(@"-?\d*\.\d+"), CultureInfo.InvariantCulture.NumberFormat);
                var value = Expression.Constant(new BsonValue(number));

                return Expression.NewArrayInit(typeof(BsonValue), value);
            }
            else if (s.Match(@"-?\d+")) // read int
            {
                var number = Convert.ToInt32(s.Scan(@"-?\d+"), CultureInfo.InvariantCulture.NumberFormat);
                var value = Expression.Constant(new BsonValue(number));

                return Expression.NewArrayInit(typeof(BsonValue), value);
            }
            else if (s.Match(@"(true|false)")) // read bool
            {
                var boolean = Convert.ToBoolean(s.Scan(@"(true|false)"));
                var value = Expression.Constant(new BsonValue(boolean));

                return Expression.NewArrayInit(typeof(BsonValue), value);
            }
            else if (s.Match(@"null")) // read null
            {
                var value = Expression.Constant(BsonValue.Null);

                return Expression.NewArrayInit(typeof(BsonValue), value);
            }
            else if (s.Match(@"'")) // read string
            {
                var str = s.Scan(@"'([\s\S]*?)'", 1);
                var value = Expression.Constant(new BsonValue(str));

                return Expression.NewArrayInit(typeof(BsonValue), value);
            }
            else if (s.Scan(@"\{\s*").Length > 0) // read document {
            {
                // read key value
                var method = typeof(ExpressionOperators).GetMethod("DOCUMENT");
                var keys = new List<Expression>();
                var values = new List<Expression>();

                while (!s.HasTerminated)
                {
                    // read key + value
                    var key = s.Scan(@"(.+?)\s*:\s*", 1).ThrowIfEmpty("Invalid token", s);
                    var value = ParseExpression(s, root, current, false);

                    // add key and value to parameter list (as an expression)
                    keys.Add(Expression.Constant(new BsonValue(key)));
                    values.Add(value);

                    if (s.Scan(@"\s*,\s*").Length > 0) continue;
                    else if (s.Scan(@"\s*\}\s*").Length > 0) break;
                    throw LiteException.SyntaxError(s);
                }

                var arrKeys = Expression.NewArrayInit(typeof(BsonValue), keys.ToArray());
                var arrValues = Expression.NewArrayInit(typeof(IEnumerable<BsonValue>), values.ToArray());

                return Expression.Call(method, new Expression[] { arrKeys, arrValues });
            }
            else if (s.Scan(@"\[\s*").Length > 0) // read array [
            {
                var method = typeof(ExpressionOperators).GetMethod("ARRAY");
                var values = new List<Expression>();

                while (!s.HasTerminated)
                {
                    // read value expression
                    var value = ParseExpression(s, root, current, false);

                    values.Add(value);

                    if (s.Scan(@"\s*,\s*").Length > 0) continue;
                    else if (s.Scan(@"\s*\]\s*").Length > 0) break;
                    throw LiteException.SyntaxError(s);
                }

                var arrValues = Expression.NewArrayInit(typeof(IEnumerable<BsonValue>), values.ToArray());

                return Expression.Call(method, new Expression[] { arrValues });
            }
            else if (s.Scan(@"\(\s*").Length > 0) // read inner (
            {
                // read a inner expression inside ( and )
                var inner = ParseExpression(s, root, current, false);

                if (s.Scan(@"\s*\)").Length == 0) throw LiteException.SyntaxError(s);

                return inner;
            }
            else if (s.Match(@"\w+\s*\(")) // read function
            {
                // get static method from this class
                var name = s.Scan(@"(\w+)\s*\(", 1).ToUpper();
                var parameters = new List<Expression>();

                if (s.Scan(@"\s*\)\s*").Length == 0)
                {
                    while (!s.HasTerminated)
                    {
                        var parameter = ParseExpression(s, root, current, false);

                        parameters.Add(parameter);

                        if (s.Scan(@"\s*,\s*").Length > 0) continue;
                        else if (s.Scan(@"\s*\)\s*").Length > 0) break;
                        throw LiteException.SyntaxError(s);
                    }
                }

                var method = _methods.FirstOrDefault(x => x.Name == name && x.GetParameters().Count() == parameters.Count);

                if (method == null) throw LiteException.SyntaxError(s, "Method " + name + " not exist or invalid parameter count");

                return Expression.Call(method, parameters.ToArray());
            }

            throw LiteException.SyntaxError(s);
        }

        /// <summary>
        /// Implement a JSON-Path like navigation on BsonDocument. Support a simple range of paths
        /// </summary>
        private static Expression ParsePath(StringScanner s, Expression expr, ParameterExpression root)
        {
            if (s.Match(@"\.[\$\-\w]+"))
            {
                var method = typeof(BsonExpression).GetMethod("Member");
                var name = Expression.Constant(s.Scan(@"\.([\$\-\w]+)", 1));
                return Expression.Call(method, expr, name);
            }
            else if (s.Match(@"\["))
            {
                var method = typeof(BsonExpression).GetMethod("Array");
                var i = s.Scan(@"\[\s*(-?[\d+\*])\s*\]", 1);
                var index = i != "*" && i != "" ? Convert.ToInt32(i) : int.MaxValue;
                var inner = new BsonExpression(null);

                if (i == "") // if array operation are not index based, read expression 
                {
                    s.Scan(@"\[\s*");
                    // read expression with full support to all operators/formulas
                    inner = ReadExpression(s, true, false, false);
                    if (inner == null) throw LiteException.SyntaxError(s, "Invalid expression formula");
                    s.Scan(@"\s*\]");
                }

                return Expression.Call(method, expr, Expression.Constant(index), Expression.Constant(inner), root);
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Expression reader from a StringScanner

        /// <summary>
        /// Extract expression or a path from a StringScanner. If required = true, throw error if is not a valid expression. If required = false, returns null for not valid expression and back Index in StringScanner to original position
        /// </summary>
        internal static BsonExpression ReadExpression(StringScanner s, bool required, bool pathOnly, bool arithmeticOnly = true)
        {
            var start = s.Index;

            try
            {
                return new BsonExpression(s, pathOnly, arithmeticOnly);
            }
            catch (LiteException ex) when (required == false && ex.ErrorCode == LiteException.SYNTAX_ERROR)
            {
                s.Index = start;
                return null;
            }
        }

        #endregion

        #region Path static methods

        /// <summary>
        /// Returns value from root document. Returns same document if name are empty
        /// </summary>
        public static IEnumerable<BsonValue> Root(BsonValue value, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                yield return value;
            }
            else if (value.IsDocument)
            {
                if (value.AsDocument.TryGetValue(name, out BsonValue item))
                {
                    // fill destroy action to remove value from root
                    item.Destroy = () => value.AsDocument.Remove(name);

                    yield return item;
                }
            }
        }

        /// <summary>
        /// Return a value from a value as document. If has no name, just return values ($). If value are not a document, do not return anything
        /// </summary>
        public static IEnumerable<BsonValue> Member(IEnumerable<BsonValue> values, string name)
        {
            foreach (var doc in values.Where(x => x.IsDocument).Select(x => x.AsDocument))
            {
                if (doc.TryGetValue(name, out BsonValue item))
                {
                    // fill destroy action to remove value from parent document
                    item.Destroy = () => doc.Remove(name);

                    yield return item;
                }
            }
        }

        /// <summary>
        /// Returns all values from array according index. If index are MaxValue, return all values
        /// </summary>
        public static IEnumerable<BsonValue> Array(IEnumerable<BsonValue> values, int index, BsonExpression expr, BsonDocument root)
        {
            foreach (var value in values)
            {
                if (value.IsArray)
                {
                    var arr = value.AsArray;

                    // [<expr>] - index are an expression
                    if (expr.Source != null)
                    {
                        foreach (var item in arr)
                        {
                            // execute for each child value and except a first bool value (returns if true)
                            var c = expr.Execute(root, item, true).First();

                            if (c.IsBoolean && c.AsBoolean == true)
                            {
                                // fill destroy action to remove value from parent array
                                item.Destroy = () => arr.Remove(item);

                                yield return item;
                            }
                        }
                    }
                    // [*] - index are all values
                    else if (index == int.MaxValue)
                    {
                        foreach (var item in arr)
                        {
                            // fill destroy action to remove value from parent array
                            item.Destroy = () => arr.Remove(item);

                            yield return item;
                        }
                    }
                    // [n] - fixed index
                    else
                    {
                        var idx = index < 0 ? arr.Count + index : index;

                        if (arr.Count > idx)
                        {
                            var item = arr[idx];

                            // fill destroy action to remove value from parent array
                            item.Destroy = () => arr.Remove(item);

                            yield return item;
                        }
                    }
                }
            }
        }

        #endregion

        public override string ToString()
        {
            return this.Source;
        }
    }
}