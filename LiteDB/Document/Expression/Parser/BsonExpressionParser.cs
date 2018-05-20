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
            ["LIKE"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("LIKE"), BsonExpressionType.Like),
            ["BETWEEN"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("BETWEEN"), BsonExpressionType.Between),
            [">"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("GT"), BsonExpressionType.GreaterThan),
            [">="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("GTE"), BsonExpressionType.GreaterThanOrEqual),
            ["<"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("LT"), BsonExpressionType.LessThan),
            ["<="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("LTE"), BsonExpressionType.LessThanOrEqual),
            ["!="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("NEQ"), BsonExpressionType.NotEqual),

            // logic
            ["OR"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("OR"), BsonExpressionType.Or),
            ["AND"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("AND"), BsonExpressionType.And)
        };

        private static MethodInfo _parameterPathMethod = typeof(BsonExpressionOperators).GetMethod("PARAMETER_PATH");
        private static MethodInfo _memberPathMethod = typeof(BsonExpressionOperators).GetMethod("MEMBER_PATH");
        private static MethodInfo _arrayPathMethod = typeof(BsonExpressionOperators).GetMethod("ARRAY_PATH");

        private static MethodInfo _documentInitMethod = typeof(BsonExpressionOperators).GetMethod("DOCUMENT_INIT");
        private static MethodInfo _arrayInitMethod = typeof(BsonExpressionOperators).GetMethod("ARRAY_INIT");

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
        /// Start parse string into linq expression. Read path, function or base type bson values (int, double, bool, string)
        /// </summary>
        public static List<BsonExpression> ParseFullExpression(Tokenizer s, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot, bool onlyTerms)
        {
            var first = ParseSingleExpression(s, root, current, parameters, isRoot);
            var values = new List<BsonExpression> { first };
            var ops = new List<string>();

            // read all blocks and operation first
            while (!s.EOF)
            {
                // read operator between expressions
                var op = s.LookAhead(true);

                // if no valid operator, stop reading string
                if (op.Type != TokenType.Operator) break;

                s.ReadToken(); // consume _ahead

                var expr = ParseSingleExpression(s, root, current, parameters, isRoot);

                // special BETWEEN "AND" read
                if (op.Value.Equals("BETWEEN", StringComparison.OrdinalIgnoreCase))
                {
                    var and = s.ReadToken(true).Expect(TokenType.Operator);

                    if (!and.Value.Equals("AND", StringComparison.OrdinalIgnoreCase)) throw LiteException.UnexpectedToken(s.Current);

                    var expr2 = ParseSingleExpression(s, root, current, parameters, isRoot);

                    // convert expr and expr2 into an array with 2 values
                    expr = NewArray(expr, expr2);
                }

                values.Add(expr);
                ops.Add(op.Value.ToUpper());
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
                        Fields = new HashSet<string>(left.Fields).AddRange(right.Fields),
                        Expression = Expression.Call(op.Value.Item1, left.Expression, right.Expression),
                        Left = left,
                        Right = right,
                        Source = left.Source + (op.Key.IsWord() ? (" " + op.Key + " ") : op.Key) + right.Source
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
        private static BsonExpression ParseSingleExpression(Tokenizer s, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            // read next token and test with all expression parts
            var token = s.ReadToken();

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
                throw LiteException.UnexpectedToken(token);
        }

        /// <summary>
        /// Try parse double number - return null if not double token
        /// </summary>
        private static BsonExpression TryParseDouble(Tokenizer s)
        {
            string value = null;

            if (s.Current.Type == TokenType.Double)
            {
                value = s.Current.Value;
            }
            else if (s.Current.Value == "-")
            {
                var ahead = s.LookAhead(false);

                if (ahead.Type == TokenType.Double)
                {
                    value = "-" + s.ReadToken().Value;
                }
            }

            if (value != null)
            {
                var number = Convert.ToDouble(value, CultureInfo.InvariantCulture.NumberFormat);
                var constant = Expression.Constant(new BsonValue(number));

                return new BsonExpression
                {
                    Type = BsonExpressionType.Double,
                    IsConstant = true,
                    IsImmutable = true,
                    Fields = new HashSet<string>(),
                    Expression = Expression.NewArrayInit(typeof(BsonValue), constant),
                    Source = number.ToString(CultureInfo.InvariantCulture.NumberFormat)
                };
            }

            return null;
        }

        /// <summary>
        /// Try parse int number - return null if not int token
        /// </summary>
        private static BsonExpression TryParseInt(Tokenizer s)
        {
            string value = null;

            if (s.Current.Type == TokenType.Int)
            {
                value = s.Current.Value;
            }
            else if (s.Current.Value == "-")
            {
                var ahead = s.LookAhead(false);

                if (ahead.Type == TokenType.Int)
                {
                    value = "-" + s.ReadToken().Value;
                }
            }

            if (value != null)
            {
                var number = Convert.ToInt32(value, CultureInfo.InvariantCulture.NumberFormat);
                var constant = Expression.Constant(new BsonValue(number));

                return new BsonExpression
                {
                    Type = BsonExpressionType.Int,
                    IsConstant = true,
                    IsImmutable = true,
                    Fields = new HashSet<string>(),
                    Expression = Expression.NewArrayInit(typeof(BsonValue), constant),
                    Source = number.ToString(CultureInfo.InvariantCulture.NumberFormat)
                };
            }

            return null;
        }

        /// <summary>
        /// Try parse bool - return null if not bool token
        /// </summary>
        private static BsonExpression TryParseBool(Tokenizer s)
        {
            if (s.Current.Type == TokenType.Word && (s.Current.Value == "true" || s.Current.Value == "false"))
            {
                var boolean = Convert.ToBoolean(s.Current.Value);
                var value = Expression.Constant(new BsonValue(boolean));

                return new BsonExpression
                {
                    Type = BsonExpressionType.Boolean,
                    IsConstant = true,
                    IsImmutable = true,
                    Fields = new HashSet<string>(),
                    Expression = Expression.NewArrayInit(typeof(BsonValue), value),
                    Source = boolean.ToString().ToLower()
                };
            }

            return null;
        }

        /// <summary>
        /// Try parse null constant - return null if not null token
        /// </summary>
        private static BsonExpression TryParseNull(Tokenizer s)
        {
            if (s.Current.Type == TokenType.Word && s.Current.Value == "null")
            {
                var value = Expression.Constant(BsonValue.Null);

                return new BsonExpression
                {
                    Type = BsonExpressionType.Null,
                    IsConstant = true,
                    IsImmutable = true,
                    Fields = new HashSet<string>(),
                    Expression = Expression.NewArrayInit(typeof(BsonValue), value),
                    Source = "null"
                };
            }

            return null;
        }

        /// <summary>
        /// Try parse string with both single/double quote - return null if not string
        /// </summary>
        private static BsonExpression TryParseString(Tokenizer s)
        {
            if (s.Current.Type == TokenType.String)
            {
                var bstr = new BsonValue(s.Current.Value);
                var value = Expression.Constant(bstr);

                return new BsonExpression
                {
                    Type = BsonExpressionType.String,
                    IsConstant = true,
                    IsImmutable = true,
                    Fields = new HashSet<string>(),
                    Expression = Expression.NewArrayInit(typeof(BsonValue), value),
                    Source = JsonSerializer.Serialize(bstr)
                };
            }

            return null;
        }

        /// <summary>
        /// Try parse json document - return null if not document token
        /// </summary>
        private static BsonExpression TryParseDocument(Tokenizer s, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (s.Current.Type != TokenType.OpenBrace) return null;

            // read key value
            var keys = new List<Expression>();
            var values = new List<Expression>();
            var source = new StringBuilder();
            var isConstant = true;
            var isImmutable = true;
            var fields = new HashSet<string>();

            source.Append("{");

            while (!s.CheckEOF())
            {
                // read simple or complex document key name
                var src = new StringBuilder(); // use another builder to re-use in simplified notation
                var key = ReadKey(s, src);

                source.Append(src);

                s.ReadToken(); // update s.Current 

                source.Append(":");

                BsonExpression value;

                // test normal notation { a: 1 }
                if (s.Current.Type == TokenType.Colon)
                {
                    value = ParseFullExpression(s, root, current, parameters, isRoot, false).Single();

                    // read next token here (, or }) because simplified version already did
                    s.ReadToken();
                }
                else
                {
                    var fname = src.ToString();

                    // support for simplified notation { a, b, c } == { a: $.a, b: $.b, c: $.c }
                    value = new BsonExpression
                    {
                        Type = BsonExpressionType.Path,
                        IsConstant = false,
                        IsImmutable = isImmutable,
                        Fields = new HashSet<string>(new string[] { key }),
                        Expression = Expression.Call(_memberPathMethod, root, Expression.Constant(key)) as Expression,
                        Source = "$." + (fname.IsWord() ? fname : "[" + fname + "]")
                    };
                }

                // update isImmutable/isConstant only when came false
                if (value.IsImmutable == false) isImmutable = false;
                if (value.IsConstant == false) isConstant = false;

                fields.AddRange(value.Fields);

                // add key and value to parameter list (as an expression)
                keys.Add(Expression.Constant(new BsonValue(key)));
                values.Add(value.Expression);

                // include value source in current source
                source.Append(value.Source);

                // test next token for , (continue) or } (break)
                s.Current.Expect(TokenType.Comma, TokenType.CloseBrace);

                source.Append(s.Current.Value);

                if (s.Current.Type == TokenType.Comma) continue; else break;
            }

            var arrKeys = Expression.NewArrayInit(typeof(BsonValue), keys.ToArray());
            var arrValues = Expression.NewArrayInit(typeof(IEnumerable<BsonValue>), values.ToArray());

            return new BsonExpression
            {
                Type = BsonExpressionType.Document,
                IsConstant = isConstant,
                IsImmutable = isImmutable,
                Fields = fields,
                Expression = Expression.Call(_documentInitMethod, new Expression[] { arrKeys, arrValues }),
                Source = source.ToString()
            };
        }

        /// <summary>
        /// Try parse array - return null if not array token
        /// </summary>
        private static BsonExpression TryParseArray(Tokenizer s, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (s.Current.Type != TokenType.OpenBracket) return null;

            var values = new List<Expression>();
            var source = new StringBuilder();
            var isConstant = true;
            var isImmutable = true;
            var fields = new HashSet<string>();

            source.Append("[");

            while (!s.CheckEOF())
            {
                // read value expression
                var value = ParseFullExpression(s, root, current, parameters, isRoot, false).Single();

                // update isImmutable/isConstant only when came false
                if (value.IsImmutable == false) isImmutable = false;
                if (value.IsConstant == false) isConstant = false;

                fields.AddRange(value.Fields);

                values.Add(value.Expression);

                var next = s.ReadToken()
                    .Expect(TokenType.Comma, TokenType.CloseBracket);

                source.Append(next.Value);

                if (next.Type == TokenType.Comma) continue; else break;
            }

            var arrValues = Expression.NewArrayInit(typeof(IEnumerable<BsonValue>), values.ToArray());

            return new BsonExpression
            {
                Type = BsonExpressionType.Array,
                IsConstant = isConstant,
                IsImmutable = isImmutable,
                Fields = fields,
                Expression = Expression.Call(_arrayInitMethod, new Expression[] { arrValues }),
                Source = source.ToString()
            };
        }

        /// <summary>
        /// Try parse parameter - return null if not parameter token
        /// </summary>
        private static BsonExpression TryParseParameter(Tokenizer s, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (s.Current.Type != TokenType.At) return null;

            var ahead = s.LookAhead(false);

            if (ahead.Type == TokenType.Word || ahead.Type == TokenType.Int)
            {
                var parameterName = s.ReadToken(false).Value;
                var name = Expression.Constant(parameterName);

                return new BsonExpression
                {
                    Type = BsonExpressionType.Parameter,
                    IsConstant = true,
                    IsImmutable = false,
                    Fields = new HashSet<string>(),
                    Expression = Expression.Call(_parameterPathMethod, parameters, name),
                    Source = "@" + parameterName
                };
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Try parse inner expression - return null if not bracket token
        /// </summary>
        private static BsonExpression TryParseInnerExpression(Tokenizer s, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (s.Current.Type != TokenType.OpenParenthesis) return null;

            // read a inner expression inside ( and )
            var inner = ParseFullExpression(s, root, current, parameters, isRoot, false).Single();

            // read close )
            s.ReadToken().Expect(TokenType.CloseParenthesis);

            return new BsonExpression
            {
                Type = inner.Type,
                IsConstant = inner.IsConstant,
                IsImmutable = inner.IsImmutable,
                Fields = inner.Fields,
                Expression = inner.Expression,
                Source = "(" + inner.Source + ")"
            };
        }

        /// <summary>
        /// Try parse method call - return null if not method call
        /// </summary>
        private static BsonExpression TryParseMethodCall(Tokenizer s, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            var token = s.Current;

            if (s.Current.Type != TokenType.Word) return null;
            if (s.LookAhead().Type != TokenType.OpenParenthesis) return null;

            // read (
            s.ReadToken();

            // get static method from this class
            var pars = new List<Expression>();
            var source = new StringBuilder();
            var isConstant = true;
            var isImmutable = true;
            var fields = new HashSet<string>();

            source.Append(token.Value.ToUpper() + "(");

            // method call with no parameters
            if (s.LookAhead().Type == TokenType.CloseBrace)
            {
                s.ReadToken(); // read )
            }
            else
            {
                while (!s.CheckEOF())
                {
                    var parameter = ParseFullExpression(s, root, current, parameters, isRoot, false).Single();

                    // update isImmutable/isConstant only when came false
                    if (parameter.IsImmutable == false) isImmutable = false;
                    if (parameter.IsConstant == false) isConstant = false;

                    // add fields from each parameters
                    fields.AddRange(parameter.Fields);

                    pars.Add(parameter.Expression);

                    // append source string
                    source.Append(parameter.Source);

                    // read , or )
                    var next = s.ReadToken()
                        .Expect(TokenType.Comma, TokenType.CloseParenthesis);

                    source.Append(next.Value);

                    if (next.Type == TokenType.Comma) continue; else break;
                }
            }

            var method = GetMethod(token.Value, pars.Count);

            if (method == null) throw LiteException.SyntaxError("Method " + token.Value + " not exist or invalid parameter count", s.Position);

            // test if method are decorated with "Variable" (immutable = false)
            if (method.GetCustomAttribute<VolatileAttribute>() != null)
            {
                isImmutable = false;
            }

            return new BsonExpression
            {
                Type = BsonExpressionType.Call,
                IsConstant = isConstant,
                IsImmutable = isImmutable,
                Fields = fields,
                Expression = Expression.Call(method, pars.ToArray()),
                Source = source.ToString()
            };
        }

        /// <summary>
        /// Parse JSON-Path - return null if not method call
        /// </summary>
        private static BsonExpression TryParsePath(Tokenizer s, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            // test $ or @ or WORD
            if (s.Current.Type != TokenType.At && s.Current.Type != TokenType.Dollar && s.Current.Type != TokenType.Word) return null;

            var scope = isRoot ? "$" : "@";

            if (s.Current.Type == TokenType.At || s.Current.Type == TokenType.Dollar)
            {
                scope = s.Current.Type == TokenType.Dollar ? "$" : "@";

                var ahead = s.LookAhead(false);

                if(ahead.Type == TokenType.Period)
                {
                    s.ReadToken(); // read .
                    s.ReadToken(); // read word or [
                }
            }

            var source = new StringBuilder();
            var isImmutable = true;

            source.Append(scope);

            // read field name (or "" if root)
            var field = ReadField(s, source);
            var name = Expression.Constant(field);
            var expr = Expression.Call(_memberPathMethod, scope == "$" ? root : current, name) as Expression;

            // parse the rest of path
            while (!s.EOF)
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
                Fields = new HashSet<string>(new string[] { field.Length == 0 ? "$" : field }),
                Expression = expr,
                Source = source.ToString()
            };
        }

        /// <summary>
        /// Implement a JSON-Path like navigation on BsonDocument. Support a simple range of paths
        /// </summary>
        private static Expression ParsePath(Tokenizer s, Expression expr, ParameterExpression root, ParameterExpression parameters, StringBuilder source, ref bool isImmutable)
        {
            var ahead = s.LookAhead(false);

            if (ahead.Type == TokenType.Period)
            {
                s.ReadToken(); // read .
                s.ReadToken(false); //

                var field = ReadField(s, source);

                var name = Expression.Constant(field);

                return Expression.Call(_memberPathMethod, expr, name);

            }
            else if (ahead.Type == TokenType.OpenBracket) // array 
            {
                source.Append("[");

                s.ReadToken(); // read [

                ahead = s.LookAhead(); // look for "index" or "expression"

                var index = int.MaxValue;
                var inner = BsonExpression.Empty;

                if (ahead.Type == TokenType.Int)
                {
                    // fixed index
                    source.Append(s.ReadToken().Value);
                    index = Convert.ToInt32(s.Current.Value);
                }
                else if (ahead.Value == "-")
                {
                    // fixed negative index
                    source.Append(s.ReadToken().Value + s.ReadToken().Expect(TokenType.Int).Value);
                    index = -Convert.ToInt32(s.Current.Value);
                }
                else if (ahead.Value == "*")
                {
                    // read all items (index = int.MaxValue)
                    s.ReadToken();
                }
                else
                {
                    // inner expression
                    inner = BsonExpression.Parse(s, false, false).FirstOrDefault();

                    if (inner == null) throw LiteException.SyntaxError("Invalid expression formula", s.Position);

                    // if array filter is not immutable, update ref (update only when false)
                    if (inner.IsImmutable == false) isImmutable = false;

                    source.Append(inner.Source);
                }

                // read ]
                s.ReadToken().Expect(TokenType.CloseBracket);

                source.Append("]");

                return Expression.Call(_arrayPathMethod, expr, Expression.Constant(index), Expression.Constant(inner), root, parameters);
            }

            return null;
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
                Fields = new HashSet<string>(item0.Fields).AddRange(item1.Fields),
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
                Fields = new HashSet<string>(left.Fields).AddRange(right.Fields),
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
        /// Get field from simple \w regex or ['comp-lex'] - also, add into source. Can read empty field (root)
        /// </summary>
        private static string ReadField(Tokenizer s, StringBuilder source)
        {
            var field = "";

            // if field are complex
            if (s.Current.Type == TokenType.OpenBracket)
            {
                field = s.ReadToken().Expect(TokenType.String).Value;
                s.ReadToken().Expect(TokenType.CloseBracket);
            }
            else if (s.Current.Type == TokenType.Word)
            {
                field = s.Current.Value;
            }

            if (field.Length > 0)
            {
                source.Append(".");

                // add bracket in result only if is complex type
                if (field.IsWord())
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
        private static string ReadKey(Tokenizer s, StringBuilder source)
        {
            var token = s.ReadToken();
            var key = "";

            if (token.Type == TokenType.String)
            {
                key = token.Value;
            }
            else
            {
                key = token.Expect(TokenType.Word, TokenType.Int).Value;
            }

            if (key.IsWord())
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