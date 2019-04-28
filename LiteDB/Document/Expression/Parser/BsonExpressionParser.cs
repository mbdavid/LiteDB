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
        private static readonly Dictionary<string, Tuple<MethodInfo, BsonExpressionType>> _operators = new Dictionary<string, Tuple<MethodInfo, BsonExpressionType>>
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

            ["ANY LIKE"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("LIKE_ANY"), BsonExpressionType.Like),
            ["ANY BETWEEN"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("BETWEEN_ANY"), BsonExpressionType.Between),
            ["ANY IN"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("IN_ANY"), BsonExpressionType.In),

            ["ANY >"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("GT_ANY"), BsonExpressionType.GreaterThan),
            ["ANY >="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("GTE_ANY"), BsonExpressionType.GreaterThanOrEqual),
            ["ANY <"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("LT_ANY"), BsonExpressionType.LessThan),
            ["ANY <="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("LTE_ANY"), BsonExpressionType.LessThanOrEqual),

            ["ANY !="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("NEQ_ANY"), BsonExpressionType.NotEqual),
            ["ANY ="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("EQ_ANY"), BsonExpressionType.Equal),

            ["ALL LIKE"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("LIKE_ALL"), BsonExpressionType.Like),
            ["ALL BETWEEN"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("BETWEEN_ALL"), BsonExpressionType.Between),
            ["ALL IN"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("IN_ALL"), BsonExpressionType.In),

            ["ALL >"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("GT_ALL"), BsonExpressionType.GreaterThan),
            ["ALL >="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("GTE_ALL"), BsonExpressionType.GreaterThanOrEqual),
            ["ALL <"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("LT_ALL"), BsonExpressionType.LessThan),
            ["ALL <="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("LTE_ALL"), BsonExpressionType.LessThanOrEqual),

            ["ALL !="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("NEQ_ALL"), BsonExpressionType.NotEqual),
            ["ALL ="] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("EQ_ALL"), BsonExpressionType.Equal),

            // logic
            ["AND"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("AND"), BsonExpressionType.And),
            ["OR"] = Tuple.Create(typeof(BsonExpressionOperators).GetMethod("OR"), BsonExpressionType.Or)
        };

        private static readonly MethodInfo _parameterPathMethod = typeof(BsonExpressionOperators).GetMethod("PARAMETER_PATH");
        private static readonly MethodInfo _memberPathMethod = typeof(BsonExpressionOperators).GetMethod("MEMBER_PATH");
        private static readonly MethodInfo _arrayIndexMethod = typeof(BsonExpressionOperators).GetMethod("ARRAY_INDEX");
        private static readonly MethodInfo _arrayFilterMethod = typeof(BsonExpressionOperators).GetMethod("ARRAY_FILTER");

        private static readonly MethodInfo _mapMethod = typeof(BsonExpressionOperators).GetMethod("MAP");

        private static readonly MethodInfo _documentInitMethod = typeof(BsonExpressionOperators).GetMethod("DOCUMENT_INIT");
        private static readonly MethodInfo _arrayInitMethod = typeof(BsonExpressionOperators).GetMethod("ARRAY_INIT");

        #endregion

        /// <summary>
        /// Start parse string into linq expression. Read path, function or base type bson values (int, double, bool, string)
        /// </summary>
        public static BsonExpression ParseFullExpression(Tokenizer tokenizer, ParameterExpression source, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            var first = ParseSingleExpression(tokenizer, source, root, current, parameters, isRoot);
            var values = new List<BsonExpression> { first };
            var ops = new List<string>();

            // read all blocks and operation first
            while (!tokenizer.EOF)
            {
                // read operator between expressions
                var op = ReadOperant(tokenizer);

                if (op == null) break;

                var expr = ParseSingleExpression(tokenizer, source, root, current, parameters, isRoot);

                // special BETWEEN "AND" read
                if (op.EndsWith("BETWEEN", StringComparison.OrdinalIgnoreCase))
                {
                    var and = tokenizer.ReadToken(true).Expect("AND");

                    var expr2 = ParseSingleExpression(tokenizer, source, root, current, parameters, isRoot);

                    // convert expr and expr2 into an array with 2 values
                    expr = NewArray(expr, expr2);
                }

                values.Add(expr);
                ops.Add(op.ToUpper());
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

                    // test left/right scalar
                    var isLeftEnum = op.Key.StartsWith("ALL") || op.Key.StartsWith("ANY");

                    if (isLeftEnum && left.IsScalar) throw new LiteException(0, $"Left expression `{left.Source}` must return multiples values");
                    if (!isLeftEnum && !right.IsScalar) throw new LiteException(0, $"Left expression `{right.Source}` must return a single value");
                    if (right.IsScalar == false) throw new LiteException(0, $"Right expression `{right.Source}` must return a single value");

                    // process result in a single value
                    var result = new BsonExpression
                    {
                        Type = op.Value.Item2,
                        IsImmutable = left.IsImmutable && right.IsImmutable,
                        IsScalar = true,
                        IsAll = op.Key.StartsWith("ALL"),
                        Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase).AddRange(left.Fields).AddRange(right.Fields),
                        Expression = Expression.Call(op.Value.Item1, left.Expression, right.Expression),
                        Left = left,
                        Right = right,
                        Source = left.Source + (" " + op.Key + " ") + right.Source
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
        public static BsonExpression ParseSingleExpression(Tokenizer tokenizer, ParameterExpression source, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            // read next token and test with all expression parts
            var token = tokenizer.ReadToken();

            return
                TryParseDouble(tokenizer) ??
                TryParseInt(tokenizer) ??
                TryParseBool(tokenizer) ??
                TryParseNull(tokenizer) ??
                TryParseString(tokenizer) ??
                TryParseSource(tokenizer, source) ??
                TryParseDocument(tokenizer, source, root, current, parameters, isRoot) ??
                TryParseArray(tokenizer, source, root, current, parameters, isRoot) ??
                TryParseParameter(tokenizer, source, root, current, parameters, isRoot) ??
                TryParseInnerExpression(tokenizer, source, root, current, parameters, isRoot) ??
                TryParseMap(tokenizer, source, root, current, parameters, isRoot) ??
                TryParseMethodCall(tokenizer, source, root, current, parameters, isRoot) ??
                TryParsePath(tokenizer, source, root, current, parameters, isRoot) ??
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
                    IsScalar = true,
                    Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    Expression = constant,
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
                    IsScalar = true,
                    Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    Expression = constant,
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
                var constant = Expression.Constant(new BsonValue(boolean));

                return new BsonExpression
                {
                    Type = BsonExpressionType.Boolean,
                    IsImmutable = true,
                    IsScalar = true,
                    Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    Expression = constant,
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
                var constant = Expression.Constant(BsonValue.Null);

                return new BsonExpression
                {
                    Type = BsonExpressionType.Null,
                    IsImmutable = true,
                    IsScalar = true,
                    Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    Expression = constant,
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
                var constant = Expression.Constant(bstr);

                return new BsonExpression
                {
                    Type = BsonExpressionType.String,
                    IsImmutable = true,
                    IsScalar = true,
                    Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    Expression = constant,
                    Source = JsonSerializer.Serialize(bstr)
                };
            }

            return null;
        }

        /// <summary>
        /// Try parse json document - return null if not document token
        /// </summary>
        private static BsonExpression TryParseDocument(Tokenizer tokenizer, ParameterExpression source, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (tokenizer.Current.Type != TokenType.OpenBrace) return null;

            // read key value
            var keys = new List<Expression>();
            var values = new List<Expression>();
            var src = new StringBuilder();
            var isImmutable = true;
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            src.Append("{");

            while (!tokenizer.CheckEOF())
            {
                // read simple or complex document key name
                var innerSrc = new StringBuilder(); // use another builder to re-use in simplified notation
                var key = ReadKey(tokenizer, innerSrc);

                src.Append(innerSrc);

                tokenizer.ReadToken(); // update s.Current 

                src.Append(":");

                BsonExpression value;

                // test normal notation { a: 1 }
                if (tokenizer.Current.Type == TokenType.Colon)
                {
                    value = ParseFullExpression(tokenizer, source, root, current, parameters, isRoot);

                    // read next token here (, or }) because simplified version already did
                    tokenizer.ReadToken();
                }
                else
                {
                    var fname = innerSrc.ToString();

                    // support for simplified notation { a, b, c } == { a: $.a, b: $.b, c: $.c }
                    value = new BsonExpression
                    {
                        Type = BsonExpressionType.Path,
                        IsImmutable = isImmutable,
                        IsScalar = true,
                        Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase).AddRange(new string[] { key }),
                        Expression = Expression.Call(_memberPathMethod, root, Expression.Constant(key)) as Expression,
                        Source = "$." + (fname.IsWord() ? fname : "[" + fname + "]")
                    };
                }

                // document value must be a scalar value
                if (value.IsScalar == false) throw new LiteException(0, $"Document value `{value.Source}` must be a scalar expression");

                // update isImmutable only when came false
                if (value.IsImmutable == false) isImmutable = false;

                fields.AddRange(value.Fields);

                // add key and value to parameter list (as an expression)
                keys.Add(Expression.Constant(key));
                values.Add(value.Expression);

                // include value source in current source
                src.Append(value.Source);

                // test next token for , (continue) or } (break)
                tokenizer.Current.Expect(TokenType.Comma, TokenType.CloseBrace);

                src.Append(tokenizer.Current.Value);

                if (tokenizer.Current.Type == TokenType.Comma) continue; else break;
            }

            var arrKeys = Expression.NewArrayInit(typeof(string), keys.ToArray());
            var arrValues = Expression.NewArrayInit(typeof(BsonValue), values.ToArray());

            return new BsonExpression
            {
                Type = BsonExpressionType.Document,
                IsImmutable = isImmutable,
                IsScalar = true,
                Fields = fields,
                Expression = Expression.Call(_documentInitMethod, new Expression[] { arrKeys, arrValues }),
                Source = src.ToString()
            };
        }

        /// <summary>
        /// Try parse source documents (when passed) * - return null if not array token
        /// </summary>
        private static BsonExpression TryParseSource(Tokenizer tokenizer, ParameterExpression source)
        {
            if (tokenizer.Current.Type != TokenType.Asterisk) return null;

            return new BsonExpression
            {
                Type = BsonExpressionType.Source,
                IsImmutable = true,
                IsScalar = false,
                Fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                Expression = source,
                Source = "*"
            };
        }

        /// <summary>
        /// Try parse array - return null if not array token
        /// </summary>
        private static BsonExpression TryParseArray(Tokenizer tokenizer, ParameterExpression source, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (tokenizer.Current.Type != TokenType.OpenBracket) return null;

            var values = new List<Expression>();
            var src = new StringBuilder();
            var isImmutable = true;
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            src.Append("[");

            while (!tokenizer.CheckEOF())
            {
                // read value expression
                var value = ParseFullExpression(tokenizer, source, root, current, parameters, isRoot);

                src.Append(value.Source);

                // update isImmutable only when came false
                if (value.IsImmutable == false) isImmutable = false;

                fields.AddRange(value.Fields);

                // include value source in current source
                values.Add(value.Expression);

                var next = tokenizer.ReadToken()
                    .Expect(TokenType.Comma, TokenType.CloseBracket);

                src.Append(next.Value);

                if (next.Type == TokenType.Comma) continue; else break;
            }

            var arrValues = Expression.NewArrayInit(typeof(BsonValue), values.ToArray());

            return new BsonExpression
            {
                Type = BsonExpressionType.Array,
                IsImmutable = isImmutable,
                IsScalar = true,
                Fields = fields,
                Expression = Expression.Call(_arrayInitMethod, arrValues),
                Source = src.ToString()
            };
        }

        /// <summary>
        /// Try parse parameter - return null if not parameter token
        /// </summary>
        private static BsonExpression TryParseParameter(Tokenizer tokenizer, ParameterExpression source, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
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
                    IsScalar = true,
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
        private static BsonExpression TryParseInnerExpression(Tokenizer tokenizer, ParameterExpression source, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (tokenizer.Current.Type != TokenType.OpenParenthesis) return null;

            // read a inner expression inside ( and )
            var inner = ParseFullExpression(tokenizer, source, root, current, parameters, isRoot);

            // read close )
            tokenizer.ReadToken().Expect(TokenType.CloseParenthesis);

            return new BsonExpression
            {
                Type = inner.Type,
                IsImmutable = inner.IsImmutable,
                IsScalar = inner.IsScalar,
                Fields = inner.Fields,
                Expression = inner.Expression,
                Left = inner.Left,
                Right = inner.Right,
                Source = "(" + inner.Source + ")"
            };
        }

        /// <summary>
        /// Try parse MAP function - return null if not method call
        /// </summary>
        private static BsonExpression TryParseMap(Tokenizer tokenizer, ParameterExpression source, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            if (!tokenizer.Current.Is("MAP")) return null;
            if (tokenizer.LookAhead().Type != TokenType.OpenParenthesis) return null;

            // read (
            tokenizer.ReadToken();

            var src = new StringBuilder("MAP(");
            var isImmutable = true;
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // read enumerable expression
            var input = ParseSingleExpression(tokenizer, source, root, current, parameters, isRoot);

            if (input.IsScalar) throw new LiteException(0, $"MAP function require an input enumerable expression");

            tokenizer.ReadToken().Expect(TokenType.Equals); // read =
            tokenizer.ReadToken().Expect(TokenType.Greater); // read >

            src.Append(input.Source);
            src.Append("=>");

            var output = BsonExpression.Parse(tokenizer, false);

            if (output == null) throw LiteException.UnexpectedToken(tokenizer.Current);

            // read last )
            tokenizer.ReadToken().Expect(TokenType.CloseParenthesis);

            if (input.IsImmutable == false) isImmutable = false;
            if (output.IsImmutable == false) isImmutable = false;

            fields.AddRange(input.Fields);
            fields.AddRange(output.Fields);

            src.Append(output.Source);
            src.Append(")");

            return new BsonExpression
            {
                Type = BsonExpressionType.Call,
                IsImmutable = isImmutable,
                IsScalar = false,
                Fields = fields,
                Expression = Expression.Call(_mapMethod, input.Expression, Expression.Constant(output), root, parameters),
                Source = src.ToString()
            };
        }

        /// <summary>
        /// Try parse method call - return null if not method call
        /// </summary>
        private static BsonExpression TryParseMethodCall(Tokenizer tokenizer, ParameterExpression source, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
        {
            var token = tokenizer.Current;

            if (tokenizer.Current.Type != TokenType.Word) return null;
            if (tokenizer.LookAhead().Type != TokenType.OpenParenthesis) return null;

            // read (
            tokenizer.ReadToken();

            // get static method from this class
            var pars = new List<BsonExpression>();
            var src = new StringBuilder();
            var isImmutable = true;
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            src.Append(token.Value.ToUpper() + "(");

            // method call with no parameters
            if (tokenizer.LookAhead().Type == TokenType.CloseParenthesis)
            {
                src.Append(tokenizer.ReadToken().Value); // read )
            }
            else
            {
                while (!tokenizer.CheckEOF())
                {
                    var parameter = ParseFullExpression(tokenizer, source, root, current, parameters, isRoot);

                    // update isImmutable only when came false
                    if (parameter.IsImmutable == false) isImmutable = false;

                    // add fields from each parameters
                    fields.AddRange(parameter.Fields);

                    pars.Add(parameter);

                    // append source string
                    src.Append(parameter.Source);

                    // read , or )
                    var next = tokenizer.ReadToken()
                        .Expect(TokenType.Comma, TokenType.CloseParenthesis);

                    src.Append(next.Value);

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

            // test parameters Scalar vs IEnumerable
            foreach (var z in method.GetParameters().Zip(pars, (l, r) => new { l, r }))
            {
                if (z.l.ParameterType.IsEnumerable() && z.r.IsScalar)
                {
                    throw new LiteException(0, $"Parameter `{z.l.Name}` in method `{method.Name}` must be an enumerable value");
                }
                if (z.l.ParameterType.IsEnumerable() == false && z.r.IsScalar == false)
                {
                    throw new LiteException(0, $"Parameter `{z.l.Name}` in method `{method.Name}` must be a scalar value");
                }
            }

            return new BsonExpression
            {
                Type = BsonExpressionType.Call,
                IsImmutable = isImmutable,
                IsScalar = method.ReturnType.IsEnumerable() == false,
                Fields = fields,
                Expression = Expression.Call(method, pars.Select(x => x.Expression).ToArray()),
                Source = src.ToString()
            };
        }

        /// <summary>
        /// Parse JSON-Path - return null if not method call
        /// </summary>
        private static BsonExpression TryParsePath(Tokenizer tokenizer, ParameterExpression source, ParameterExpression root, ParameterExpression current, ParameterExpression parameters, bool isRoot)
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

            var src = new StringBuilder();
            var isImmutable = true;
            var isScalar = true;
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            src.Append(scope == TokenType.Dollar ? "$" : "@");

            // read field name (or "" if root)
            var field = ReadField(tokenizer, src);
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
                var result = ParsePath(tokenizer, expr, source, root, parameters, fields, ref isImmutable, ref isScalar, src);

                if (isScalar == false)
                {
                    expr = result;
                    break;
                }

                // filter method must exit
                if (result == null) break;

                expr = result;
            }

            return new BsonExpression
            {
                Type = BsonExpressionType.Path,
                IsImmutable = isImmutable,
                IsScalar = isScalar,
                Fields = fields,
                Expression = expr,
                Source = src.ToString()
            };
        }

        /// <summary>
        /// Implement a JSON-Path like navigation on BsonDocument. Support a simple range of paths
        /// </summary>
        private static Expression ParsePath(Tokenizer tokenizer, Expression expr, ParameterExpression source, ParameterExpression root, ParameterExpression parameters, HashSet<string> fields, ref bool isImmutable, ref bool isScalar, StringBuilder src)
        {
            var ahead = tokenizer.LookAhead(false);

            if (ahead.Type == TokenType.Period)
            {
                tokenizer.ReadToken(); // read .
                tokenizer.ReadToken(false); //

                var field = ReadField(tokenizer, src);

                var name = Expression.Constant(field);

                return Expression.Call(_memberPathMethod, expr, name);
            }
            else if (ahead.Type == TokenType.OpenBracket) // array 
            {
                src.Append("[");

                tokenizer.ReadToken(); // read [

                ahead = tokenizer.LookAhead(); // look for "index" or "expression"

                var index = int.MaxValue;
                var inner = BsonExpression.Empty;
                var method = _arrayIndexMethod;

                if (ahead.Type == TokenType.Int)
                {
                    // fixed index
                    src.Append(tokenizer.ReadToken().Value);
                    index = Convert.ToInt32(tokenizer.Current.Value);
                }
                else if (ahead.Type == TokenType.Minus)
                {
                    // fixed negative index
                    src.Append(tokenizer.ReadToken().Value + tokenizer.ReadToken().Expect(TokenType.Int).Value);
                    index = -Convert.ToInt32(tokenizer.Current.Value);
                }
                else if (ahead.Type == TokenType.Asterisk)
                {
                    // all items * (index = MaxValue)
                    method = _arrayFilterMethod;
                    isScalar = false;

                    src.Append(tokenizer.ReadToken().Value);
                }
                else
                {
                    // inner expression
                    inner = BsonExpression.Parse(tokenizer, false);

                    if (inner == null) throw LiteException.UnexpectedToken(tokenizer.Current);

                    // if array filter is not immutable, update ref (update only when false)
                    if (inner.IsImmutable == false) isImmutable = false;

                    // if inner expression returns a single parameter, still Scalar
                    // otherwise it's an operand filter expression (enumerable)
                    if (inner.Type != BsonExpressionType.Parameter)
                    {
                        method = _arrayFilterMethod;
                        isScalar = false;
                    }

                    // add inner fields (can contains root call)
                    fields.AddRange(inner.Fields);

                    src.Append(inner.Source);
                }

                // read ]
                tokenizer.ReadToken().Expect(TokenType.CloseBracket);

                src.Append("]");

                return Expression.Call(method, expr, Expression.Constant(index), Expression.Constant(inner), root, parameters);
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

            // both values must be scalar expressions
            if (item0.IsScalar == false) throw new LiteException(0, $"Expression `{item0.Source}` must be a scalar expression");
            if (item1.IsScalar == false) throw new LiteException(0, $"Expression `{item0.Source}` must be a scalar expression");

            var arrValues = Expression.NewArrayInit(typeof(BsonValue), values.ToArray());

            return new BsonExpression
            {
                Type = BsonExpressionType.Array,
                IsImmutable = isImmutable,
                IsScalar = true,
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
                IsScalar = left.IsScalar && right.IsScalar,
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

        /// <summary>
        /// Read next token as Operant with ANY|ALL keyword before - returns null if next token are not an operant
        /// </summary>
        private static string ReadOperant(Tokenizer tokenizer)
        {
            var token = tokenizer.LookAhead(true);

            if (token.IsOperand)
            {
                tokenizer.ReadToken(); // consume operant
                return token.Value;
            }

            if (token.Is("ALL") || token.Is("ANY"))
            {
                var key = token.Value.ToUpper();

                tokenizer.ReadToken(); // consume operant

                token = tokenizer.ReadToken();

                if (token.IsOperand == false) throw LiteException.UnexpectedToken("Expected valid operant", token);

                return key + " " + token.Value;
            }

            return null;
        }
    }
}