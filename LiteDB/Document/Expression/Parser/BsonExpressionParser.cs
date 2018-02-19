using System;
using System.Collections;
using System.Collections.Concurrent;
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
    internal class BsonExpressionParser
    {
        #region Operators quick access

        /// <summary>
        /// Operation definition by methods with defined expression type
        /// </summary>
        private static Dictionary<string, Tuple<MethodInfo, BsonExpressionType>> _operators = new Dictionary<string, Tuple<MethodInfo, BsonExpressionType>>
        {
            // arithmetic
            ["%"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("MOD"), BsonExpressionType.Modulo),
            ["/"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("DIVIDE"), BsonExpressionType.Divide),
            ["*"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("MULTIPLY"), BsonExpressionType.Multiply),
            ["+"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("ADD"), BsonExpressionType.Add),
            ["-"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("MINUS"), BsonExpressionType.Subtract),

            // conditional
            ["="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("EQ"), BsonExpressionType.Equal),
            [" LIKE "] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("LIKE"), BsonExpressionType.Like),
            [" BETWEEN "] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("BETWEEN"), BsonExpressionType.Between),
            [">"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("GT"), BsonExpressionType.GreaterThan),
            [">="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("GTE"), BsonExpressionType.GreaterThanOrEqual),
            ["<"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("LT"), BsonExpressionType.LessThan),
            ["<="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("LTE"), BsonExpressionType.LessThanOrEqual),
            ["!="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("NEQ"), BsonExpressionType.NotEqual),

            // logic
            [" OR "] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("OR"), BsonExpressionType.Or),
            [" AND "] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("AND"), BsonExpressionType.And)
        };

        private static MethodInfo _rootPathMethod = typeof(BsonExpressionOperators).GetMethod("ROOT_PATH");
        private static MethodInfo _memberPathMethod = typeof(BsonExpressionOperators).GetMethod("MEMBER_PATH");
        private static MethodInfo _arrayPathMethod = typeof(BsonExpressionOperators).GetMethod("ARRAY_PATH");

        private static MethodInfo _documentInitMethod = typeof(BsonExpressionOperators).GetMethod("DOCUMENT_INIT");
        private static MethodInfo _arrayInitMethod = typeof(BsonExpressionOperators).GetMethod("ARRAY_INIT");

        private static Regex RE_OPERATORS = new Regex(@"^\s*(\+|\-|\*|\/|%|=|\sLIKE\s|\sBETWEEN\s|!=|>=|>|<=|<|\sOR\s|\sAND\s)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        #endregion

        #region Regular expression definitions

        private static Regex RE_SIMPLE_FIELD = new Regex(@"^[$\w]+$", RegexOptions.Compiled);

        #endregion

        #region MethodCall quick access

        /// <summary>
        /// Load all static methods from BsonExpressionMethods class. Use a dictionary using name + parameter count
        /// </summary>
        private static Dictionary<string, MethodInfo> _methods =
            typeof(BsonExpressionMethods).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .ToDictionary(m => m.Name.ToUpper() + "~" + m.GetParameters().Length);

        /// <summary>
        /// Get expression method with same name and same parameter - return null if not found
        /// </summary>
        private static MethodInfo GetMethod(string name, int parameterCount)
        {
            var key = name.ToUpper() + "~" + parameterCount;

            return _methods.GetOrDefault(key);
        }

        #endregion

        /// <summary>
        /// Extract expression from StringScanner. If required = true, throw error if is not a valid expression. If required = false, returns null for not valid expression and back Index in StringScanner to original position
        /// </summary>
        public static BsonExpression ReadExpression(StringScanner s, bool required)
        {
            var start = s.Index;

            try
            {
                return BsonExpression.Parse(s, false).Single();
            }
            catch (LiteException ex) when (required == false && ex.ErrorCode == LiteException.SYNTAX_ERROR)
            {
                s.Index = start;
                return null;
            }
        }

        /// <summary>
        /// Start parse string into linq expression. Read path, function or base type bson values (int, double, bool, string)
        /// </summary>
        public static List<BsonExpression> ParseFullExpression(StringScanner s, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot, bool onlyTerms)
        {
            var first = ParseSingleExpression(s, root, current, parameters, isRoot);
            var values = new List<BsonExpression> { first };
            var ops = new List<string>();

            // read all blocks and operation first
            while (!s.HasTerminated)
            {
                // read operator between expressions
                var op = s.Scan(RE_OPERATORS, 1).ToUpper();

                // if no valid operator, stop reading string
                if (op.Length == 0) break;

                var expr = ParseSingleExpression(s, root, current, parameters, isRoot);

                // special BETWEEN "AND" read
                if (op == " BETWEEN ")
                {
                    s.Scan(@"\s+AND\s+").ThrowIfEmpty("Missing AND statement on BETWEEN", s);

                    var expr2 = ParseSingleExpression(s, root, current, parameters, isRoot);

                    // convert expr and expr2 into an array with 2 values
                    expr = NewArray(expr, expr2);
                }

                values.Add(expr);
                ops.Add(op);
            }

            var order = 0;
            var andOperator = _operators.Count - 1; // last operator are AND

            // now, process operator in correct order
            while (values.Count >= 2 && (onlyTerms == false || order < andOperator))
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
                    var result = new BsonExpression
                    {
                        Type = op.Value.Item2,
                        IsConstant = left.IsConstant && right.IsConstant,
                        IsImmutable = left.IsImmutable && right.IsImmutable,
                        Expression = Expression.Call(op.Value.Item1, left.Expression, right.Expression),
                        Left = left,
                        Right = right,
                        Source = left.Source + op.Key + right.Source
                    };

                    // remove left+right and insert result
                    values.Insert(n, result);
                    values.RemoveRange(n + 1, 2);

                    // remove operation
                    ops.RemoveAt(n);
                }
            }

            return values;
        }

        /// <summary>
        /// Start parse string into linq expression. Read path, function or base type bson values (int, double, bool, string)
        /// </summary>
        private static BsonExpression ParseSingleExpression(StringScanner s, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            return
                TryParseDouble(s) ??
                TryParseInt(s) ??
                TryParseBool(s) ??
                TryParseNull(s) ??
                TryParseString(s) ??
                TryParseDocument(s, root, current, parameters, isRoot) ??
                TryParseArray(s, root, current, parameters, isRoot) ??
                TryParseParameter(s, root, current, parameters, isRoot) ??
                TryParseInnerExpression(s, root, current, parameters, isRoot) ??
                TryParseMethodCall(s, root, current, parameters, isRoot) ??
                TryParsePath(s, root, current, parameters, isRoot) ??
                throw LiteException.SyntaxError(s);
        }

        /// <summary>
        /// Try parse double number - return null if not double token
        /// </summary>
        private static BsonExpression TryParseDouble(StringScanner s)
        {
            if (!s.Scan(@"-?\s*\d*\.\d+", out var doubleNumber)) return null;

            var number = Convert.ToDouble(doubleNumber, CultureInfo.InvariantCulture.NumberFormat);
            var value = Expression.Constant(new BsonValue(number));

            return new BsonExpression
            {
                Type = BsonExpressionType.Double,
                IsConstant = true,
                IsImmutable = true,
                Expression = Expression.NewArrayInit(typeof(BsonValue), value),
                Source = number.ToString(CultureInfo.InvariantCulture.NumberFormat)
            };
        }

        /// <summary>
        /// Try parse int number - return null if not int token
        /// </summary>
        private static BsonExpression TryParseInt(StringScanner s)
        {
            if (!s.Scan(@"-?\s*\d+", out var intNumber)) return null;

            var number = Convert.ToInt32(intNumber, CultureInfo.InvariantCulture.NumberFormat);
            var value = Expression.Constant(new BsonValue(number));

            return new BsonExpression
            {
                Type = BsonExpressionType.Int,
                IsConstant = true,
                IsImmutable = true,
                Expression = Expression.NewArrayInit(typeof(BsonValue), value),
                Source = number.ToString(CultureInfo.InvariantCulture.NumberFormat)
            };
        }

        /// <summary>
        /// Try parse bool - return null if not bool token
        /// </summary>
        private static BsonExpression TryParseBool(StringScanner s)
        {
            if (!s.Scan(@"(true|false)", out var boolValue)) return null;

            var boolean = Convert.ToBoolean(boolValue);
            var value = Expression.Constant(new BsonValue(boolean));

            return new BsonExpression
            {
                Type = BsonExpressionType.Boolean,
                IsConstant = true,
                IsImmutable = true,
                Expression = Expression.NewArrayInit(typeof(BsonValue), value),
                Source = boolean.ToString().ToLower()
            };
        }

        /// <summary>
        /// Try parse null constant - return null if not null token
        /// </summary>
        private static BsonExpression TryParseNull(StringScanner s)
        {
            if (s.Scan(@"null").Length == 0) return null;

            var value = Expression.Constant(BsonValue.Null);

            return new BsonExpression
            {
                Type = BsonExpressionType.Null,
                IsConstant = true,
                IsImmutable = true,
                Expression = Expression.NewArrayInit(typeof(BsonValue), value),
                Source = "null"
            };
        }

        /// <summary>
        /// Try parse string with both single/double quote - return null if not string
        /// </summary>
        private static BsonExpression TryParseString(StringScanner s)
        {
            if (!s.Scan(@"['""]", out var quote)) return null;

            var str = s.ReadString(quote == "'" ? '\'' : '"');
            var bstr = new BsonValue(str);
            var value = Expression.Constant(bstr);

            return new BsonExpression
            {
                Type = BsonExpressionType.String,
                IsConstant = true,
                IsImmutable = true,
                Expression = Expression.NewArrayInit(typeof(BsonValue), value),
                Source = JsonSerializer.Serialize(bstr)
            };
        }

        /// <summary>
        /// Try parse json document - return null if not document token
        /// </summary>
        private static BsonExpression TryParseDocument(StringScanner s, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (s.Scan(@"\{\s*").Length == 0) return null;

            // read key value
            var keys = new List<Expression>();
            var values = new List<Expression>();
            var source = new StringBuilder();
            var isConstant = true;
            var isImmutable = true;

            source.Append("{");

            while (!s.HasTerminated)
            {
                // read simple or complex document key name
                var key = ReadKey(s, source).ThrowIfEmpty("Invalid token", s);

                source.Append(":");

                // test if is simplified document notation { a, b, c } == { a: $.a, b: $.b, c: $.c }
                var simplified = s.Match(@"\s*([,\}])");
                var scanner = s;

                if (simplified)
                {
                    scanner = new StringScanner("$.['" + key + "']");
                }
                else
                {
                    // default notation = key: value - read : here
                    s.Scan(@"\s*:\s*").ThrowIfEmpty("Missing : after key", s);
                }

                // read value by parsing as expression
                var value = ParseFullExpression(scanner, root, current, parameters, isRoot, false).Single();

                // update isImmutable/isConstant only when came false
                if (value.IsImmutable == false) isImmutable = false;
                if (value.IsConstant == false) isConstant = false;

                // add key and value to parameter list (as an expression)
                keys.Add(Expression.Constant(new BsonValue(key)));
                values.Add(value.Expression);

                // include value source in current source
                source.Append(value.Source);

                if (s.Scan(@"\s*,\s*").Length > 0)
                {
                    source.Append(",");
                    continue;
                }
                else if (s.Scan(@"\s*\}\s*").Length > 0) break;
                throw LiteException.SyntaxError(s);
            }

            source.Append("}");

            var arrKeys = Expression.NewArrayInit(typeof(BsonValue), keys.ToArray());
            var arrValues = Expression.NewArrayInit(typeof(IEnumerable<BsonValue>), values.ToArray());

            return new BsonExpression
            {
                Type = BsonExpressionType.Document,
                IsConstant = isConstant,
                IsImmutable = isImmutable,
                Expression = Expression.Call(_documentInitMethod, new Expression[] { arrKeys, arrValues }),
                Source = source.ToString()
            };
        }

        /// <summary>
        /// Try parse array - return null if not array token
        /// </summary>
        private static BsonExpression TryParseArray(StringScanner s, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (s.Scan(@"\[\s*").Length == 0) return null;

            var values = new List<Expression>();
            var source = new StringBuilder();
            var isConstant = true;
            var isImmutable = true;

            source.Append("[");

            while (!s.HasTerminated)
            {
                // read value expression
                var value = ParseFullExpression(s, root, current, parameters, isRoot, false).Single();

                // update isImmutable/isConstant only when came false
                if (value.IsImmutable == false) isImmutable = false;
                if (value.IsConstant == false) isConstant = false;

                values.Add(value.Expression);

                if (s.Scan(@"\s*,\s*").Length > 0)
                {
                    source.Append(",");
                    continue;
                }
                else if (s.Scan(@"\s*\]\s*").Length > 0) break;
                throw LiteException.SyntaxError(s);
            }

            source.Append("]");

            var arrValues = Expression.NewArrayInit(typeof(IEnumerable<BsonValue>), values.ToArray());

            return new BsonExpression
            {
                Type = BsonExpressionType.Array,
                IsConstant = isConstant,
                IsImmutable = isImmutable,
                Expression = Expression.Call(_arrayInitMethod, new Expression[] { arrValues }),
                Source = source.ToString()
            };
        }

        /// <summary>
        /// Try parse parameter - return null if not parameter token
        /// </summary>
        private static BsonExpression TryParseParameter(StringScanner s, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (!s.Scan(@"\@(\w+)", 1, out var parameterName)) return null;

            var name = Expression.Constant(parameterName);

            return new BsonExpression
            {
                Type = BsonExpressionType.Parameter,
                IsConstant = true,
                IsImmutable = false,
                Expression = Expression.Call(_rootPathMethod, parameters, name),
                Source = "@" + parameterName
            };
        }

        /// <summary>
        /// Try parse inner expression - return null if not bracket token
        /// </summary>
        private static BsonExpression TryParseInnerExpression(StringScanner s, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (s.Scan(@"\(\s*").Length == 0) return null;

            // read a inner expression inside ( and )
            var inner = ParseFullExpression(s, root, current, parameters, isRoot, false).Single();

            if (s.Scan(@"\s*\)").Length == 0) throw LiteException.SyntaxError(s);

            return new BsonExpression
            {
                Type = inner.Type,
                IsConstant = inner.IsConstant,
                IsImmutable = inner.IsImmutable,
                Expression = inner.Expression,
                Source = "(" + inner.Source + ")"
            };
        }

        /// <summary>
        /// Try parse method call - return null if not method call
        /// </summary>
        private static BsonExpression TryParseMethodCall(StringScanner s, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (!s.Scan(@"(\w+)\s*\(\s*", 1, out var methodName)) return null;

            // get static method from this class
            var pars = new List<Expression>();
            var source = new StringBuilder();
            var isConstant = true;
            var isImmutable = true;

            source.Append(methodName.ToUpper() + "(");

            if (s.Scan(@"\s*\)\s*").Length == 0)
            {
                while (!s.HasTerminated)
                {
                    var parameter = ParseFullExpression(s, root, current, parameters, isRoot, false).Single();

                    // update isImmutable/isConstant only when came false
                    if (parameter.IsImmutable == false) isImmutable = false;
                    if (parameter.IsConstant == false) isConstant = false;

                    pars.Add(parameter.Expression);

                    // append source string
                    source.Append(parameter.Source);

                    if (s.Scan(@"\s*,\s*").Length > 0)
                    {
                        source.Append(",");
                        continue;
                    }
                    else if (s.Scan(@"\s*\)\s*").Length > 0) break;
                    throw LiteException.SyntaxError(s);
                }
            }

            source.Append(")");

            var method = GetMethod(methodName, pars.Count);

            if (method == null) throw LiteException.SyntaxError(s, "Method " + methodName + " not exist or invalid parameter count");

            // test if method are decorated with "Variable" (immutable = false)
            if (method.GetCustomAttribute<VariableAttribute>() != null)
            {
                isImmutable = false;
            }

            return new BsonExpression
            {
                Type = BsonExpressionType.Call,
                IsConstant = isConstant,
                IsImmutable = isImmutable,
                Expression = Expression.Call(method, pars.ToArray()),
                Source = source.ToString()
            };
        }

        /// <summary>
        /// Try parse JSON-Path - return null if not method call
        /// </summary>
        private static BsonExpression TryParsePath(StringScanner s, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            //TODO precisa arrumar aqui: não pode aceitar $teste ou @campo (sem ponto)
            var scope = s.Scan(@"([\$\@])\.?", 1);

            // test if stats with $/@
            if (!s.Scan(@"(\[\s*['""]|[\$\w]+)", out var field) && scope.Length == 0) return null;

            var source = new StringBuilder();
            var isImmutable = true;

            scope = scope.TrimToNull() ?? (isRoot ? "$" : "@");

            source.Append(scope);

            field = ReadField(s, field, source);

            var name = Expression.Constant(field);
            var expr = Expression.Call(_rootPathMethod, scope == "$" ? root : current, name) as Expression;

            // parse the rest of path
            while (!s.HasTerminated)
            {
                var result = ParsePath(s, expr, root, parameters, source, ref isImmutable);

                if (result == null) break;

                expr = result;
            }

            return new BsonExpression
            {
                Type = BsonExpressionType.Path,
                IsConstant = false,
                IsImmutable = isImmutable,
                Expression = expr,
                Source = source.ToString()
            };
        }

        /// <summary>
        /// Implement a JSON-Path like navigation on BsonDocument. Support a simple range of paths
        /// </summary>
        private static Expression ParsePath(StringScanner s, Expression expr, ParameterExpression root, ParameterExpression parameters, StringBuilder source, ref bool isImmutable)
        {
            if (s.Scan(@"\.(\[\s*['""]|[\$\w]+)", 1, out var field))
            {
                field = ReadField(s, field, source);

                var name = Expression.Constant(field);

                return Expression.Call(_memberPathMethod, expr, name);
            }
            else if (s.Scan(@"\[\s*").Length > 0)
            {
                source.Append("[");

                var i = s.Scan(@"\s*(-?\s*[\d+\*])\s*\]", 1);
                var index = i != "*" && i != "" ? Convert.ToInt32(i) : int.MaxValue;
                var inner = BsonExpression.Empty;

                if (i == "") // if array operation are not index based, read expression 
                {
                    // read expression with full support to all operators/formulas
                    inner = ReadExpression(s, true);

                    if (inner == null) throw LiteException.SyntaxError(s, "Invalid expression formula");

                    // if array filter is not immutable, update ref (update only when false)
                    if (inner.IsImmutable == false) isImmutable = false;

                    source.Append(inner.Source);

                    s.Scan(@"\s*\]");
                }
                else
                {
                    source.Append(i);
                }

                source.Append("]");

                return Expression.Call(_arrayPathMethod, expr, Expression.Constant(index), Expression.Constant(inner), root, parameters);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Create an array expression with 2 values (used only in BETWEEN statement)
        /// </summary>
        private static BsonExpression NewArray(BsonExpression item0, BsonExpression item1)
        {
            var values = new Expression[] { item0.Expression, item1.Expression };
            var isConstant = item0.IsConstant && item1.IsConstant;    
            var isImmutable = item0.IsImmutable && item1.IsImmutable;

            var arrValues = Expression.NewArrayInit(typeof(IEnumerable<BsonValue>), values.ToArray());

            return new BsonExpression
            {
                Type = BsonExpressionType.Array,
                IsConstant = isConstant,
                IsImmutable = isImmutable,
                Expression = Expression.Call(_arrayInitMethod, new Expression[] { arrValues }),
                Source = item0.Source + " AND " + item1.Source
            };
        }

        /// <summary>
        /// Create new binary expression based in 2 sides expression
        /// </summary>
        internal static BsonExpression CreateBinaryExpression(string op, BsonExpression left, BsonExpression right)
        {
            // create new binary expression based in 2 other expressions
            var result = new BsonExpression
            {
                Type = _operators[op].Item2,
                IsConstant = left.IsConstant && right.IsConstant,
                IsImmutable = left.IsImmutable && right.IsImmutable,
                Expression = Expression.Call(_operators[op].Item1, left.Expression, right.Expression),
                Left = left,
                Right = right,
                Source = left.Source + op + right.Source
            };

            // copy their parameters into result
            left.Parameters.CopyTo(result.Parameters);
            right.Parameters.CopyTo(result.Parameters);

            return result;
        }

        /// <summary>
        /// Get field from simples \w regex or ['comp-lex'] - also, add into source
        /// </summary>
        private static string ReadField(StringScanner s, string field, StringBuilder source)
        {
            // if field are complex
            if (field.StartsWith("["))
            {
                field = s.ReadString(field.EndsWith("'") ? '\'' : '"');
                s.Scan(@"\s*\]");
            }

            if (field.Length > 0)
            {
                source.Append(".");

                if (RE_SIMPLE_FIELD.IsMatch(field))
                {
                    source.Append(field);
                }
                else
                {
                    source.Append("[");
                    JsonSerializer.Serialize(field, source);
                    source.Append("]");
                }
            }

            return field;
        }

        /// <summary>
        /// Read key in document definition with single word or "comp-lex"
        /// </summary>
        private static string ReadKey(StringScanner s, StringBuilder source)
        {
            var key = s.Scan(@"(['""]|[$\w]+)");

            // if key are complex, read as string
            if (key == "'" || key == "\"")
            {
                key = s.ReadString(key == "'" ? '\'' : '"');
            }

            if (RE_SIMPLE_FIELD.IsMatch(key))
            {
                source.Append(key);
            }
            else
            {
                JsonSerializer.Serialize(key, source);
            }

            return key;
        }

    }
}