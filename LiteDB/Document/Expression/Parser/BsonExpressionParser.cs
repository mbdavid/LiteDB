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
        /// Operation definition by methods
        /// </summary>
        private static Dictionary<string, MethodInfo> _operators = new Dictionary<string, MethodInfo>
        {
            ["%"] = typeof(BsonExpressionOperators).GetMethod("MOD"),
            ["/"] = typeof(BsonExpressionOperators).GetMethod("DIVIDE"),
            ["*"] = typeof(BsonExpressionOperators).GetMethod("MULTIPLY"),
            ["+"] = typeof(BsonExpressionOperators).GetMethod("ADD"),
            ["-"] = typeof(BsonExpressionOperators).GetMethod("MINUS"),
            [">"] = typeof(BsonExpressionOperators).GetMethod("GT"),
            [">="] = typeof(BsonExpressionOperators).GetMethod("GTE"),
            ["<"] = typeof(BsonExpressionOperators).GetMethod("LT"),
            ["<="] = typeof(BsonExpressionOperators).GetMethod("LTE"),
            ["="] = typeof(BsonExpressionOperators).GetMethod("EQ"),
            ["!="] = typeof(BsonExpressionOperators).GetMethod("NEQ"),
            //["startswith"] = typeof(ExpressionOperators).GetMethod("STARTSWITH"),
            //["endswith"] = typeof(ExpressionOperators).GetMethod("ENDSWITH"),
            //["between"] = typeof(ExpressionOperators).GetMethod("BETWEEN"),
            [" OR "] = typeof(BsonExpressionOperators).GetMethod("OR"),
            [" AND "] = typeof(BsonExpressionOperators).GetMethod("AND")
        };

        private static MethodInfo _rootPathMethod = typeof(ExpressionAccess).GetMethod("ROOT");
        private static MethodInfo _memberPathMethod = typeof(ExpressionAccess).GetMethod("MEMBER");
        private static MethodInfo _arrayPathMethod = typeof(ExpressionAccess).GetMethod("ARRAY");

        private static MethodInfo _documentInitMethod = typeof(ExpressionAccess).GetMethod("DOCUMENT");
        private static MethodInfo _arrayInitMethod = typeof(ExpressionAccess).GetMethod("ARRAY");

        #endregion

        #region Regular expression definitions

        /// <summary>
        /// + - * / = > ...
        /// </summary>
        private static Regex RE_OPERATORS = new Regex(@"^\s*(\+|\-|\*|\/|%|=|!=|>=|>|<=|<|\sAND\s|\sOR\s)\s*", RegexOptions.Compiled);
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
        public static List<BsonExpression> ParseFullExpression(StringScanner s, ParameterExpression root, ParameterExpression current, bool isRoot, bool onlyTerms)
        {
            var first = ParseSingleExpression(s, root, current, isRoot);
            var values = new List<BsonExpression> { first };
            var ops = new List<string>();

            // read all blocks and operation first
            while (!s.HasTerminated)
            {
                // read operator between expressions
                var op = s.Scan(RE_OPERATORS, 1);

                // if no valid operator, stop reading string
                if (op.Length == 0) break;

                var expr = ParseSingleExpression(s, root, current, isRoot);

                values.Add(expr);
                ops.Add(op);
            }

            var order = 0;
            var andOperator = _operators.Count - 1; // last operator are AND

            // now, process operator in correct order
            while (values.Count >= 2 || (onlyTerms && order == andOperator))
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
                    var conditional = op.Key.Trim();

                    // process result in a single value
                    var result = new BsonExpression
                    {
                        Type = 
                            conditional == "OR" ? BsonExpressionType.Or :
                            conditional == "AND" ? BsonExpressionType.And : BsonExpressionType.Conditional,
                        IsConstant = left.IsConstant && right.IsConstant,
                        IsImmutable = left.IsImmutable && right.IsImmutable,
                        Conditional = conditional,
                        Expression = Expression.Call(op.Value, left.Expression, right.Expression),
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
        private static BsonExpression ParseSingleExpression(StringScanner s, ParameterExpression root, ParameterExpression current, bool isRoot)
        {
            return
                TryParseDouble(s) ??
                TryParseInt(s) ??
                TryParseBool(s) ??
                TryParseNull(s) ??
                TryParseString(s) ??
                TryParseDocument(s, root, current, isRoot) ??
                TryParseArray(s, root, current, isRoot) ??
                TryParseInnerExpression(s, root, current, isRoot) ??
                TryParseMethodCall(s, root, current, isRoot) ??
                TryParsePath(s, root, current, isRoot) ??
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
                Type = BsonExpressionType.Constant,
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
                Type = BsonExpressionType.Constant,
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
                Type = BsonExpressionType.Constant,
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
                Type = BsonExpressionType.Constant,
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
                Type = BsonExpressionType.Constant,
                IsConstant = true,
                IsImmutable = true,
                Expression = Expression.NewArrayInit(typeof(BsonValue), value),
                Source = JsonSerializer.Serialize(bstr)
            };
        }

        /// <summary>
        /// Try parse json document - return null if not document token
        /// </summary>
        private static BsonExpression TryParseDocument(StringScanner s, ParameterExpression root, ParameterExpression current, bool isRoot)
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
                var value = ParseFullExpression(scanner, root, current, isRoot, false).Single();

                // update isImmutable/isConstant only when came false
                if (value.IsImmutable == false) isImmutable = false;
                if (value.IsConstant == false) isConstant = false;

                // add key and value to parameter list (as an expression)
                keys.Add(Expression.Constant(new BsonValue(key)));
                values.Add(value.Expression);

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
        private static BsonExpression TryParseArray(StringScanner s, ParameterExpression root, ParameterExpression current, bool isRoot)
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
                var value = ParseFullExpression(s, root, current, isRoot, false).Single();

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
        /// Try parse inner expression - return null if not bracket token
        /// </summary>
        private static BsonExpression TryParseInnerExpression(StringScanner s, ParameterExpression root, ParameterExpression current, bool isRoot)
        {
            if (s.Scan(@"\(\s*").Length == 0) return null;

            // read a inner expression inside ( and )
            var inner = ParseFullExpression(s, root, current, isRoot, false).Single();

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
        private static BsonExpression TryParseMethodCall(StringScanner s, ParameterExpression root, ParameterExpression current, bool isRoot)
        {
            if (!s.Scan(@"(\w+)\s*\(", 1, out var methodName)) return null;

            // get static method from this class
            var parameters = new List<Expression>();
            var source = new StringBuilder();
            var isConstant = true;
            var isImmutable = true;

            source.Append(methodName.ToUpper() + "(");

            if (s.Scan(@"\s*\)\s*").Length == 0)
            {
                while (!s.HasTerminated)
                {
                    var parameter = ParseFullExpression(s, root, current, isRoot, false).Single();

                    // update isImmutable/isConstant only when came false
                    if (parameter.IsImmutable == false) isImmutable = false;
                    if (parameter.IsConstant == false) isConstant = false;

                    parameters.Add(parameter.Expression);

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

            var method = GetMethod(methodName, parameters.Count);

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
                Expression = Expression.Call(method, parameters.ToArray()),
                Source = source.ToString()
            };
        }

        /// <summary>
        /// Try parse JSON-Path - return null if not method call
        /// </summary>
        private static BsonExpression TryParsePath(StringScanner s, ParameterExpression root, ParameterExpression current, bool isRoot)
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
                var result = ParsePath(s, expr, root, source, ref isImmutable);

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
        private static Expression ParsePath(StringScanner s, Expression expr, ParameterExpression root, StringBuilder source, ref bool isImmutable)
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

                return Expression.Call(_arrayPathMethod, expr, Expression.Constant(index), Expression.Constant(inner), root);
            }
            else
            {
                return null;
            }
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