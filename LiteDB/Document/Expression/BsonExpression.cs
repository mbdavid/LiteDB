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
        #region Ctor

        private Func<BsonDocument, BsonValue, IEnumerable<BsonValue>> _func;

        private StringBuilder _source = new StringBuilder();

        private bool _isPath = true;

        private bool _isEmpty = false;

        /// <summary>
        /// Get expression formatted
        /// </summary>
        public string Source => _source.ToString();

        /// <summary>
        /// Get if this expression are a simple path only (no operators)
        /// </summary>
        public bool IsPath => _isPath;

        /// <summary>
        /// Get if this expression are empty (return same document when executed)
        /// </summary>
        public bool IsEmpty => _isEmpty;

        public BsonExpression()
        {
            _isEmpty = true;
        }

        public BsonExpression(string expression)
            : this(new StringScanner(expression))
        {
        }

        internal BsonExpression(StringScanner s)
        {
            _func = this.Compile(s);
        }

        #endregion

        #region Execute

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
            if (_isEmpty)
            {
                yield return root;
            }
            else
            {
                var index = 0;
                var values = _func(root, current ?? root);

                foreach (var value in values)
                {
                    index++;
                    yield return value;
                }

                if (index == 0 && includeNullIfEmpty) yield return BsonValue.Null;
            }
        }

        #endregion

        #region Compiler

        /// <summary>
        /// Parse and compile an expression from a stringscanner. Must define if will read a path only or support for full expression. Can parse only arithmetic (+/-/*/..) or full logic operators (=/!=/>/...)
        /// </summary>
        private Func<BsonDocument, BsonValue, IEnumerable<BsonValue>> Compile(StringScanner s)
        {
            var root = Expression.Parameter(typeof(BsonDocument), "root");
            var current = Expression.Parameter(typeof(BsonValue), "current");

            var expr = this.ParseFullExpression(s, root, current, true);

            var lambda = Expression.Lambda<Func<BsonDocument, BsonValue, IEnumerable<BsonValue>>>(expr, root, current);

            return lambda.Compile();
        }

        #endregion

        #region Expression Parser

        /// <summary>
        /// Start parse string into linq expression. Read path, function or base type bson values (int, double, bool, string)
        /// </summary>
        private Expression ParseFullExpression(StringScanner s, ParameterExpression root, ParameterExpression current, bool isRoot)
        {
            var first = this.ParseSingleExpression(s, root, current, isRoot);
            var values = new List<Expression> { first };
            var ops = new List<string>();

            // read all blocks and operation first
            while (!s.HasTerminated)
            {
                // read operator between expressions
                var op = s.Scan(RE_OPERATORS, 1);

                // if no valid operator, stop reading string
                if (op.Length == 0) break;

                // if has operator are not a parse in root document
                if (isRoot) _isPath = false;

                _source.AppendFormat(op);

                var expr = this.ParseSingleExpression(s, root, current, isRoot);

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
        private Expression ParseSingleExpression(StringScanner s, ParameterExpression root, ParameterExpression current, bool isRoot)
        {
            if (s.Scan(@"-?\s*\d*\.\d+", out var doubleNumber)) // read double
            {
                if (isRoot) _isPath = false;

                var number = Convert.ToDouble(doubleNumber, CultureInfo.InvariantCulture.NumberFormat);
                var value = Expression.Constant(new BsonValue(number));

                _source.Append(number.ToString(CultureInfo.InvariantCulture.NumberFormat));

                return Expression.NewArrayInit(typeof(BsonValue), value);
            }
            else if (s.Scan(@"-?\s*\d+", out var intNumber)) // read int
            {
                if (isRoot) _isPath = false;

                var number = Convert.ToInt32(intNumber, CultureInfo.InvariantCulture.NumberFormat);
                var value = Expression.Constant(new BsonValue(number));

                _source.Append(number.ToString(CultureInfo.InvariantCulture.NumberFormat));

                return Expression.NewArrayInit(typeof(BsonValue), value);
            }
            else if (s.Scan(@"(true|false)", out var boolValue)) // read bool
            {
                if (isRoot) _isPath = false;

                var boolean = Convert.ToBoolean(boolValue);
                var value = Expression.Constant(new BsonValue(boolean));

                _source.Append(boolean.ToString().ToLower());

                return Expression.NewArrayInit(typeof(BsonValue), value);
            }
            else if (s.Scan(@"null").Length > 0) // read null
            {
                if (isRoot) _isPath = false;

                var value = Expression.Constant(BsonValue.Null);

                _source.Append("null");

                return Expression.NewArrayInit(typeof(BsonValue), value);
            }
            else if (s.Scan(@"['""]", out var quote)) // read string
            {
                if (isRoot) _isPath = false;

                var str = s.ReadString(quote == "'" ? '\'' : '"');
                var bstr = new BsonValue(str);
                var value = Expression.Constant(bstr);

                JsonSerializer.Serialize(bstr, _source);

                return Expression.NewArrayInit(typeof(BsonValue), value);
            }
            else if (s.Scan(@"\{\s*").Length > 0) // read document {
            {
                if (isRoot) _isPath = false;

                // read key value
                var method = typeof(ExpressionOperators).GetMethod("DOCUMENT");
                var keys = new List<Expression>();
                var values = new List<Expression>();

                _source.Append("{");

                while (!s.HasTerminated)
                {
                    // read simple or complex document key name
                    var key = this.ReadKey(s).ThrowIfEmpty("Invalid token", s);

                    // read separetor between key and value
                    s.Scan(@"\s*:\s*"); 

                    _source.Append(":");

                    // read value by parsing as expression
                    var value = this.ParseFullExpression(s, root, current, isRoot);

                    // add key and value to parameter list (as an expression)
                    keys.Add(Expression.Constant(new BsonValue(key)));
                    values.Add(value);

                    if (s.Scan(@"\s*,\s*").Length > 0)
                    {
                        _source.Append(",");
                        continue;
                    }
                    else if (s.Scan(@"\s*\}\s*").Length > 0) break;
                    throw LiteException.SyntaxError(s);
                }

                _source.Append("}");

                var arrKeys = Expression.NewArrayInit(typeof(BsonValue), keys.ToArray());
                var arrValues = Expression.NewArrayInit(typeof(IEnumerable<BsonValue>), values.ToArray());

                return Expression.Call(method, new Expression[] { arrKeys, arrValues });
            }
            else if (s.Scan(@"\[\s*").Length > 0) // read array [
            {
                if (isRoot) _isPath = false;

                var method = typeof(ExpressionOperators).GetMethod("ARRAY");
                var values = new List<Expression>();

                _source.Append("[");

                while (!s.HasTerminated)
                {
                    // read value expression
                    var value = this.ParseFullExpression(s, root, current, isRoot);

                    values.Add(value);

                    if (s.Scan(@"\s*,\s*").Length > 0)
                    {
                        _source.Append(",");
                        continue;
                    }
                    else if (s.Scan(@"\s*\]\s*").Length > 0) break;
                    throw LiteException.SyntaxError(s);
                }

                _source.Append("]");

                var arrValues = Expression.NewArrayInit(typeof(IEnumerable<BsonValue>), values.ToArray());

                return Expression.Call(method, new Expression[] { arrValues });
            }
            else if (s.Scan(@"\(\s*").Length > 0) // read inner (
            {
                if (isRoot) _isPath = false;

                _source.Append("(");

                // read a inner expression inside ( and )
                var inner = this.ParseFullExpression(s, root, current, isRoot);

                if (s.Scan(@"\s*\)").Length == 0) throw LiteException.SyntaxError(s);

                _source.Append(")");

                return inner;
            }
            else if (s.Scan(@"(\w+)\s*\(", 1, out var methodName)) // read function
            {
                if (isRoot) _isPath = false;

                // get static method from this class
                var parameters = new List<Expression>();

                _source.Append(methodName.ToUpper() + "(");

                if (s.Scan(@"\s*\)\s*").Length == 0)
                {
                    while (!s.HasTerminated)
                    {
                        var parameter = this.ParseFullExpression(s, root, current, false);

                        parameters.Add(parameter);

                        if (s.Scan(@"\s*,\s*").Length > 0)
                        {
                            _source.Append(",");
                            continue;
                        }
                        else if (s.Scan(@"\s*\)\s*").Length > 0) break;
                        throw LiteException.SyntaxError(s);
                    }
                }

                _source.Append(")");

                var method = GetMethod(methodName, parameters.Count);

                if (method == null) throw LiteException.SyntaxError(s, "Method " + methodName + " not exist or invalid parameter count");

                return Expression.Call(method, parameters.ToArray());
            }

            //TODO precisa arrumar aqui: não pode aceitar $teste ou @campo (sem ponto)

            // read path scope ($ root or @ current)
            var scope = s.Scan(@"([\$\@])\.?", 1);

            if (s.Scan(@"(\[\s*['""]|[\$\w]+)", out var field) || scope.Length > 0) // read start path
            {
                scope = scope.TrimToNull() ?? (isRoot ? "$" : "@");

                _source.Append(scope);

                field = this.ReadField(s, field);

                var name = Expression.Constant(field);
                var expr = Expression.Call(_rootMethod, scope == "$" ? root : current, name) as Expression;

                // parse the rest of path
                while (!s.HasTerminated)
                {
                    var result = this.ParsePath(s, expr, root);

                    if (result == null) break;

                    expr = result;
                }

                return expr;
            }

            throw LiteException.SyntaxError(s);
        }

        /// <summary>
        /// Implement a JSON-Path like navigation on BsonDocument. Support a simple range of paths
        /// </summary>
        private Expression ParsePath(StringScanner s, Expression expr, ParameterExpression root)
        {
            if (s.Scan(@"\.(\[\s*['""]|[\$\w]+)", 1, out var field))
            {
                field = this.ReadField(s, field);

                var name = Expression.Constant(field);

                return Expression.Call(_memberMethod, expr, name);
            }
            else if (s.Scan(@"\[\s*").Length > 0)
            {
                _source.Append("[");

                var i = s.Scan(@"\s*(-?\s*[\d+\*])\s*\]", 1);
                var index = i != "*" && i != "" ? Convert.ToInt32(i) : int.MaxValue;
                var inner = BsonExpression.Empty;

                if (i == "") // if array operation are not index based, read expression 
                {
                    // read expression with full support to all operators/formulas
                    inner = ReadExpression(s, true);

                    if (inner == null) throw LiteException.SyntaxError(s, "Invalid expression formula");

                    _source.Append(inner._source);

                    s.Scan(@"\s*\]");
                }
                else
                {
                    _source.Append(i);
                }

                _source.Append("]");

                return Expression.Call(_arrayMethod, expr, Expression.Constant(index), Expression.Constant(inner), root);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get field from simples \w regex or ['comp-lex'] - also, add into source
        /// </summary>
        private string ReadField(StringScanner s, string field)
        {
            // if field are complex
            if (field.StartsWith("["))
            {
                field = s.ReadString(field.EndsWith("'") ? '\'' : '"');
                s.Scan(@"\s*\]");
            }

            if (field.Length > 0)
            {
                _source.Append(".");

                if (RE_SIMPLE_FIELD.IsMatch(field))
                {
                    _source.Append(field);
                }
                else
                {
                    _source.Append("[");
                    JsonSerializer.Serialize(field, _source);
                    _source.Append("]");
                }
            }

            return field;
        }

        /// <summary>
        /// Read key in document definition with single word or "comp-lex"
        /// </summary>
        private string ReadKey(StringScanner s)
        {
            var key = s.Scan(@"(['""]|[$\w]+)");

            // if key are complex, read as string
            if (key == "'" || key == "\"")
            {
                key = s.ReadString(key == "'" ? '\'' : '"');
            }

            if (RE_SIMPLE_FIELD.IsMatch(key))
            {
                _source.Append(key);
            }
            else
            {
                JsonSerializer.Serialize(key, _source);
            }

            return key;
        }


        #endregion

        public override string ToString()
        {
            return _source.ToString();
        }
    }
}