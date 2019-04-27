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
        /// Operation definition by methods with defined expression type (operators are in precedence order)
        /// </summary>
        private static Dictionary<string, Tuple<MethodInfo, BsonExpressionType>> _operators = new Dictionary<string, Tuple<MethodInfo, BsonExpressionType>>
        {
            // arithmetic
            ["%"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("MOD"), BsonExpressionType.Modulo),
            ["/"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("DIVIDE"), BsonExpressionType.Divide),
            ["*"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("MULTIPLY"), BsonExpressionType.Multiply),
            ["+"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("ADD"), BsonExpressionType.Add),
            ["-"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("MINUS"), BsonExpressionType.Subtract),

            // predicate
            ["LIKE"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("LIKE"), BsonExpressionType.Like),
            ["BETWEEN"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("BETWEEN"), BsonExpressionType.Between),
            ["IN"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("IN"), BsonExpressionType.In),

            [">"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("GT"), BsonExpressionType.GreaterThan),
            [">="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("GTE"), BsonExpressionType.GreaterThanOrEqual),
            ["<"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("LT"), BsonExpressionType.LessThan),
            ["<="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("LTE"), BsonExpressionType.LessThanOrEqual),

            ["!="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("NEQ"), BsonExpressionType.NotEqual),
            ["="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("EQ"), BsonExpressionType.Equal),

            // logic
            ["AND"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("AND"), BsonExpressionType.And),
            ["OR"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("OR"), BsonExpressionType.Or)
        };

        private static MethodInfo _parameterPathMethod = typeof(BsonExpressionOperators).GetMethod("PARAMETER_PATH");
        private static MethodInfo _memberPathMethod = typeof(BsonExpressionOperators).GetMethod("MEMBER_PATH");
        private static MethodInfo _arrayPathMethod = typeof(BsonExpressionOperators).GetMethod("ARRAY_PATH");

        private static MethodInfo _documentInitMethod = typeof(BsonExpressionOperators).GetMethod("DOCUMENT_INIT");
        private static MethodInfo _arrayInitMethod = typeof(BsonExpressionOperators).GetMethod("ARRAY_INIT");

        #endregion

        /// <summary>
        /// Start parse string into linq expression. Read path, function or base type bson values (int, double, bool, string)
        /// </summary>
        public static BsonExpression ParseFullExpression(Tokenizer tokenizer, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            var first = ParseSingleExpression(tokenizer, root, current, parameters, isRoot);
            var values = new List<BsonExpression> { first };
            var ops = new List<string>();

            // read all blocks and operation first
            while (!tokenizer.EOF)
            {
                // read operator between expressions
                var op = tokenizer.LookAhead(true);

                // if no valid operator, stop reading string
                if (op.IsOperand == false) break;

                tokenizer.ReadToken(); // consume _ahead

                var expr = ParseSingleExpression(tokenizer, root, current, parameters, isRoot);

                // special BETWEEN "AND" read
                if (op.Is("BETWEEN"))
                {
                    var and = tokenizer.ReadToken(true).Expect("AND");

                    var expr2 = ParseSingleExpression(tokenizer, root, current, parameters, isRoot);

                    // convert expr and expr2 into an array with 2 values
                    expr = NewArray(expr, expr2);
                }

                values.Add(expr);
                ops.Add(op.Value.ToUpper());
            }

            var order = 0;

            // now, process operator in correct order
            while (values.Count >= 2)
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

                    // when operation is AND/OR, test if both sides are predicates (or and/or)
                    if (op.Value.Item2 == BsonExpressionType.And || op.Value.Item2 == BsonExpressionType.Or)
                    {
                        if (!(left.IsPredicate || left.Type == BsonExpressionType.And || left.Type == BsonExpressionType.Or)) throw LiteException.InvalidExpressionTypePredicate(left);
                        if (!(right.IsPredicate || right.Type == BsonExpressionType.And || right.Type == BsonExpressionType.Or)) throw LiteException.InvalidExpressionTypePredicate(right);
                    }

                    // process result in a single value
                    var result = new BsonExpression
                    {
                        Type = op.Value.Item2,
                        IsImmutable = left.IsImmutable && right.IsImmutable,
                        Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase).AddRange(left.Fields).AddRange(right.Fields),
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

            return values.Single();
        }

        /// <summary>
        /// Start parse string into linq expression. Read path, function or base type bson values (int, double, bool, string)
        /// </summary>
        private static BsonExpression ParseSingleExpression(Tokenizer tokenizer, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            // read next token and test with all expression parts
            var token = tokenizer.ReadToken();

            return
                TryParseDouble(tokenizer) ??
                TryParseInt(tokenizer) ??
                TryParseBool(tokenizer) ??
                TryParseNull(tokenizer) ??
                TryParseString(tokenizer) ??
                TryParseDocument(tokenizer, root, current, parameters, isRoot) ??
                TryParseArray(tokenizer, root, current, parameters, isRoot) ??
                TryParseParameter(tokenizer, root, current, parameters, isRoot) ??
                TryParseInnerExpression(tokenizer, root, current, parameters, isRoot) ??
                TryParseMethodCall(tokenizer, root, current, parameters, isRoot) ??
                TryParsePath(tokenizer, root, current, parameters, isRoot) ??
                throw LiteException.UnexpectedToken(token);
        }

        /// <summary>
        /// Try parse double number - return null if not double token
        /// </summary>
        private static BsonExpression TryParseDouble(Tokenizer tokenizer)
        {
            string value = null;

            if (tokenizer.Current.Type == TokenType.Double)
            {
                value = tokenizer.Current.Value;
            }
            else if (tokenizer.Current.Type == TokenType.Minus)
            {
                var ahead = tokenizer.LookAhead(false);

                if (ahead.Type == TokenType.Double)
                {
                    value = "-" + tokenizer.ReadToken().Value;
                }
            }

            if (value != null)
            {
                var number = Convert.ToDouble(value, CultureInfo.InvariantCulture.NumberFormat);
                var constant = Expression.Constant(new BsonValue(number));

                return new BsonExpression
                {
                    Type = BsonExpressionType.Double,
                    IsImmutable = true,
                    Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    Expression = Expression.NewArrayInit(typeof(BsonValue), constant),
                    Source = number.ToString(CultureInfo.InvariantCulture.NumberFormat)
                };
            }

            return null;
        }

        /// <summary>
        /// Try parse int number - return null if not int token
        /// </summary>
        private static BsonExpression TryParseInt(Tokenizer tokenizer)
        {
            string value = null;

            if (tokenizer.Current.Type == TokenType.Int)
            {
                value = tokenizer.Current.Value;
            }
            else if (tokenizer.Current.Type == TokenType.Minus)
            {
                var ahead = tokenizer.LookAhead(false);

                if (ahead.Type == TokenType.Int)
                {
                    value = "-" + tokenizer.ReadToken().Value;
                }
            }

            if (value != null)
            {
                var number = Convert.ToInt32(value, CultureInfo.InvariantCulture.NumberFormat);
                var constant = Expression.Constant(new BsonValue(number));

                return new BsonExpression
                {
                    Type = BsonExpressionType.Int,
                    IsImmutable = true,
                    Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    Expression = Expression.NewArrayInit(typeof(BsonValue), constant),
                    Source = number.ToString(CultureInfo.InvariantCulture.NumberFormat)
                };
            }

            return null;
        }

        /// <summary>
        /// Try parse bool - return null if not bool token
        /// </summary>
        private static BsonExpression TryParseBool(Tokenizer tokenizer)
        {
            if (tokenizer.Current.Type == TokenType.Word && (tokenizer.Current.Is("true") || tokenizer.Current.Is("false")))
            {
                var boolean = Convert.ToBoolean(tokenizer.Current.Value);
                var value = Expression.Constant(new BsonValue(boolean));

                return new BsonExpression
                {
                    Type = BsonExpressionType.Boolean,
                    IsImmutable = true,
                    Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    Expression = Expression.NewArrayInit(typeof(BsonValue), value),
                    Source = boolean.ToString().ToLower()
                };
            }

            return null;
        }

        /// <summary>
        /// Try parse null constant - return null if not null token
        /// </summary>
        private static BsonExpression TryParseNull(Tokenizer tokenizer)
        {
            if (tokenizer.Current.Type == TokenType.Word && tokenizer.Current.Is("null"))
            {
                var value = Expression.Constant(BsonValue.Null);

                return new BsonExpression
                {
                    Type = BsonExpressionType.Null,
                    IsImmutable = true,
                    Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    Expression = Expression.NewArrayInit(typeof(BsonValue), value),
                    Source = "null"
                };
            }

            return null;
        }

        /// <summary>
        /// Try parse string with both single/double quote - return null if not string
        /// </summary>
        private static BsonExpression TryParseString(Tokenizer tokenizer)
        {
            if (tokenizer.Current.Type == TokenType.String)
            {
                var bstr = new BsonValue(tokenizer.Current.Value);
                var value = Expression.Constant(bstr);

                return new BsonExpression
                {
                    Type = BsonExpressionType.String,
                    IsImmutable = true,
                    Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    Expression = Expression.NewArrayInit(typeof(BsonValue), value),
                    Source = JsonSerializer.Serialize(bstr)
                };
            }

            return null;
        }

        /// <summary>
        /// Try parse json document - return null if not document token
        /// </summary>
        private static BsonExpression TryParseDocument(Tokenizer tokenizer, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (tokenizer.Current.Type != TokenType.OpenBrace) return null;

            // read key value
            var keys = new List<Expression>();
            var values = new List<BsonExpression>();
            var source = new StringBuilder();
            var isImmutable = true;
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            source.Append("{");

            while (!tokenizer.CheckEOF())
            {
                // read simple or complex document key name
                var src = new StringBuilder(); // use another builder to re-use in simplified notation
                var key = ReadKey(tokenizer, src);

                source.Append(src);

                tokenizer.ReadToken(); // update s.Current 

                source.Append(":");

                BsonExpression value;

                // test normal notation { a: 1 }
                if (tokenizer.Current.Type == TokenType.Colon)
                {
                    value = ParseFullExpression(tokenizer, root, current, parameters, isRoot);

                    // read next token here (, or }) because simplified version already did
                    tokenizer.ReadToken();
                }
                else
                {
                    var fname = src.ToString();

                    // support for simplified notation { a, b, c } == { a: $.a, b: $.b, c: $.c }
                    value = new BsonExpression
                    {
                        Type = BsonExpressionType.Path,
                        IsImmutable = isImmutable,
                        Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase).AddRange(new string[] { key }),
                        Expression = Expression.Call(_memberPathMethod, root, Expression.Constant(key)) as Expression,
                        Source = "$." + (fname.IsWord() ? fname : "[" + fname + "]")
                    };
                }

                // update isImmutable only when came false
                if (value.IsImmutable == false) isImmutable = false;

                fields.AddRange(value.Fields);

                // add key and value to parameter list (as an expression)
                keys.Add(Expression.Constant(key));
                values.Add(value);

                // include value source in current source
                source.Append(value.Source);

                // test next token for , (continue) or } (break)
                tokenizer.Current.Expect(TokenType.Comma, TokenType.CloseBrace);

                source.Append(tokenizer.Current.Value);

                if (tokenizer.Current.Type == TokenType.Comma) continue; else break;
            }

            var arrKeys = Expression.NewArrayInit(typeof(string), keys.ToArray());
            var arrValues = Expression.NewArrayInit(typeof(IEnumerable<BsonValue>), values.Select(x => x.Expression).ToArray());
            var arrSources = Expression.NewArrayInit(typeof(string), values.Select(x => Expression.Constant(x.Source)).ToArray());

            return new BsonExpression
            {
                Type = BsonExpressionType.Document,
                IsImmutable = isImmutable,
                Fields = fields,
                Expression = Expression.Call(_documentInitMethod, new Expression[] { arrKeys, arrValues, arrSources }),
                Source = source.ToString()
            };
        }

        /// <summary>
        /// Try parse array - return null if not array token
        /// </summary>
        private static BsonExpression TryParseArray(Tokenizer tokenizer, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (tokenizer.Current.Type != TokenType.OpenBracket) return null;

            var values = new List<Expression>();
            var source = new StringBuilder();
            var isImmutable = true;
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            source.Append("[");

            while (!tokenizer.CheckEOF())
            {
                // read value expression
                var value = ParseFullExpression(tokenizer, root, current, parameters, isRoot);

                source.Append(value.Source);

                // update isImmutable only when came false
                if (value.IsImmutable == false) isImmutable = false;

                fields.AddRange(value.Fields);

                // include value source in current source
                values.Add(value.Expression);

                var next = tokenizer.ReadToken()
                    .Expect(TokenType.Comma, TokenType.CloseBracket);

                source.Append(next.Value);

                if (next.Type == TokenType.Comma) continue; else break;
            }

            var arrValues = Expression.NewArrayInit(typeof(IEnumerable<BsonValue>), values.ToArray());

            return new BsonExpression
            {
                Type = BsonExpressionType.Array,
                IsImmutable = isImmutable,
                Fields = fields,
                Expression = Expression.Call(_arrayInitMethod, new Expression[] { arrValues }),
                Source = source.ToString()
            };
        }

        /// <summary>
        /// Try parse parameter - return null if not parameter token
        /// </summary>
        private static BsonExpression TryParseParameter(Tokenizer tokenizer, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (tokenizer.Current.Type != TokenType.At) return null;

            var ahead = tokenizer.LookAhead(false);

            if (ahead.Type == TokenType.Word || ahead.Type == TokenType.Int)
            {
                var parameterName = tokenizer.ReadToken(false).Value;
                var name = Expression.Constant(parameterName);

                return new BsonExpression
                {
                    Type = BsonExpressionType.Parameter,
                    IsImmutable = false,
                    Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
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
        private static BsonExpression TryParseInnerExpression(Tokenizer tokenizer, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (tokenizer.Current.Type != TokenType.OpenParenthesis) return null;

            // read a inner expression inside ( and )
            var inner = ParseFullExpression(tokenizer, root, current, parameters, isRoot);

            // read close )
            tokenizer.ReadToken().Expect(TokenType.CloseParenthesis);

            return new BsonExpression
            {
                Type = inner.Type,
                IsImmutable = inner.IsImmutable,
                Fields = inner.Fields,
                Expression = inner.Expression,
                Left = inner.Left,
                Right = inner.Right,
                Source = "(" + inner.Source + ")"
            };
        }

        /// <summary>
        /// Try parse method call - return null if not method call
        /// </summary>
        private static BsonExpression TryParseMethodCall(Tokenizer tokenizer, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            var token = tokenizer.Current;

            if (tokenizer.Current.Type != TokenType.Word) return null;
            if (tokenizer.LookAhead().Type != TokenType.OpenParenthesis) return null;

            // read (
            tokenizer.ReadToken();

            // get static method from this class
            var pars = new List<Expression>();
            var source = new StringBuilder();
            var isImmutable = true;
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            source.Append(token.Value.ToUpper() + "(");

            // method call with no parameters
            if (tokenizer.LookAhead().Type == TokenType.CloseParenthesis)
            {
                source.Append(tokenizer.ReadToken().Value); // read )
            }
            else
            {
                while (!tokenizer.CheckEOF())
                {
                    var parameter = ParseFullExpression(tokenizer, root, current, parameters, isRoot);

                    // update isImmutable only when came false
                    if (parameter.IsImmutable == false) isImmutable = false;

                    // add fields from each parameters
                    fields.AddRange(parameter.Fields);

                    pars.Add(parameter.Expression);

                    // append source string
                    source.Append(parameter.Source);

                    // read , or )
                    var next = tokenizer.ReadToken()
                        .Expect(TokenType.Comma, TokenType.CloseParenthesis);

                    source.Append(next.Value);

                    if (next.Type == TokenType.Comma) continue; else break;
                }
            }

            var method = BsonExpression.GetMethod(token.Value, pars.Count);

            if (method == null) throw LiteException.UnexpectedToken($"Method '{token.Value.ToUpper()}' does not exist or contains invalid parameters", token);

            // test if method are decorated with "Variable" (immutable = false)
            if (method.GetCustomAttribute<VolatileAttribute>() != null)
            {
                isImmutable = false;
            }

            return new BsonExpression
            {
                Type = BsonExpressionType.Call,
                IsImmutable = isImmutable,
                Fields = fields,
                Expression = Expression.Call(method, pars.ToArray()),
                Source = source.ToString()
            };
        }

        /// <summary>
        /// Parse JSON-Path - return null if not method call
        /// </summary>
        private static BsonExpression TryParsePath(Tokenizer tokenizer, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            // test $ or @ or WORD
            if (tokenizer.Current.Type != TokenType.At && tokenizer.Current.Type != TokenType.Dollar && tokenizer.Current.Type != TokenType.Word) return null;

            var scope = isRoot ? TokenType.Dollar : TokenType.At;

            if (tokenizer.Current.Type == TokenType.At || tokenizer.Current.Type == TokenType.Dollar)
            {
                scope = tokenizer.Current.Type;

                var ahead = tokenizer.LookAhead(false);

                if(ahead.Type == TokenType.Period)
                {
                    tokenizer.ReadToken(); // read .
                    tokenizer.ReadToken(); // read word or [
                }
            }

            var source = new StringBuilder();
            var isImmutable = true;
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            source.Append(scope == TokenType.Dollar ? "$" : "@");

            // read field name (or "" if root)
            var field = ReadField(tokenizer, source);
            var name = Expression.Constant(field);
            var expr = Expression.Call(_memberPathMethod, scope == TokenType.Dollar ? root : current, name) as Expression;

            // add as field only if working with root document
            if (scope == TokenType.Dollar)
            {
                fields.Add(field.Length == 0 ? "$" : field);
            }

            // parse the rest of path
            while (!tokenizer.EOF)
            {
                var result = ParsePath(tokenizer, expr, root, parameters, fields, ref isImmutable, source);

                if (result == null) break;

                expr = result;
            }

            return new BsonExpression
            {
                Type = BsonExpressionType.Path,
                IsImmutable = isImmutable,
                Fields = fields,
                Expression = expr,
                Source = source.ToString()
            };
        }

        /// <summary>
        /// Implement a JSON-Path like navigation on BsonDocument. Support a simple range of paths
        /// </summary>
        private static Expression ParsePath(Tokenizer tokenizer, Expression expr, ParameterExpression root, ParameterExpression parameters, HashSet<string> fields, ref bool isImmutable, StringBuilder source)
        {
            var ahead = tokenizer.LookAhead(false);

            if (ahead.Type == TokenType.Period)
            {
                tokenizer.ReadToken(); // read .
                tokenizer.ReadToken(false); //

                var field = ReadField(tokenizer, source);

                var name = Expression.Constant(field);

                return Expression.Call(_memberPathMethod, expr, name);

            }
            else if (ahead.Type == TokenType.OpenBracket) // array 
            {
                source.Append("[");

                tokenizer.ReadToken(); // read [

                ahead = tokenizer.LookAhead(); // look for "index" or "expression"

                var index = int.MaxValue;
                var inner = BsonExpression.Empty;

                if (ahead.Type == TokenType.Int)
                {
                    // fixed index
                    source.Append(tokenizer.ReadToken().Value);
                    index = Convert.ToInt32(tokenizer.Current.Value);
                }
                else if (ahead.Type == TokenType.Minus)
                {
                    // fixed negative index
                    source.Append(tokenizer.ReadToken().Value + tokenizer.ReadToken().Expect(TokenType.Int).Value);
                    index = -Convert.ToInt32(tokenizer.Current.Value);
                }
                else if (ahead.Type == TokenType.Asterisk)
                {
                    // read all items (index = int.MaxValue)
                    source.Append(tokenizer.ReadToken().Value);
                }
                else
                {
                    // inner expression
                    inner = BsonExpression.Parse(tokenizer, false);

                    if (inner == null) throw LiteException.UnexpectedToken(tokenizer.Current);

                    // if array filter is not immutable, update ref (update only when false)
                    if (inner.IsImmutable == false) isImmutable = false;

                    // add inner fields (can contains root call)
                    fields.AddRange(inner.Fields);

                    source.Append(inner.Source);
                }

                // read ]
                tokenizer.ReadToken().Expect(TokenType.CloseBracket);

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
            var isImmutable = item0.IsImmutable && item1.IsImmutable;

            var arrValues = Expression.NewArrayInit(typeof(IEnumerable<BsonValue>), values.ToArray());

            return new BsonExpression
            {
                Type = BsonExpressionType.Array,
                IsImmutable = isImmutable,
                Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase).AddRange(item0.Fields).AddRange(item1.Fields),
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
                IsImmutable = left.IsImmutable && right.IsImmutable,
                Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase).AddRange(left.Fields).AddRange(right.Fields),
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
        private static string ReadField(Tokenizer tokenizer, StringBuilder source)
        {
            var field = "";

            // if field are complex
            if (tokenizer.Current.Type == TokenType.OpenBracket)
            {
                field = tokenizer.ReadToken().Expect(TokenType.String).Value;
                tokenizer.ReadToken().Expect(TokenType.CloseBracket);
            }
            else if (tokenizer.Current.Type == TokenType.Word)
            {
                field = tokenizer.Current.Value;
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
        private static string ReadKey(Tokenizer tokenizer, StringBuilder source)
        {
            var token = tokenizer.ReadToken();
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