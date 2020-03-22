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
using static LiteDB.Constants;

namespace LiteDB
{
    /// <summary>
    /// Compile and execute string expressions using BsonDocuments. Used in all document manipulation (transform, filter, indexes, updates). See https://github.com/mbdavid/LiteDB/wiki/Expressions
    /// </summary>
    public sealed class BsonExpression
    {
        /// <summary>
        /// Get formatted expression
        /// </summary>
        public string Source { get; internal set; }

        /// <summary>
        /// Indicate expression type
        /// </summary>
        public BsonExpressionType Type { get; internal set; }

        /// <summary>
        /// If true, this expression do not change if same document/paramter are passed (only few methods change - like NOW() - or parameters)
        /// </summary>
        public bool IsImmutable { get; internal set; }

        /// <summary>
        /// Get/Set parameter values that will be used on expression execution
        /// </summary>
        public BsonDocument Parameters { get; private set; } = new BsonDocument();

        /// <summary>
        /// In predicate expressions, indicate Left side
        /// </summary>
        internal BsonExpression Left { get; set; }

        /// <summary>
        /// In predicate expressions, indicate Rigth side
        /// </summary>
        internal BsonExpression Right { get; set; }

        /// <summary>
        /// Get/Set this expression (or any inner expression) use global Source (*)
        /// </summary>
        internal bool UseSource { get; set; }

        /// <summary>
        /// Get transformed LINQ expression
        /// </summary>
        internal Expression Expression { get; set; }

        /// <summary>
        /// Fill this hashset with all fields used in root level of document (be used to partial deserialize) - "$" means all fields
        /// </summary>
        public HashSet<string> Fields { get; internal set; }

        /// <summary>
        /// Indicate if this expressions returns a single value or IEnumerable value
        /// </summary>
        public bool IsScalar { get; internal set; }

        /// <summary>
        /// Indicate that expression evaluate to TRUE or FALSE (=, >, ...). OR and AND are not considered Predicate expressions
        /// Predicate expressions must have Left/Right expressions
        /// </summary>
        internal bool IsPredicate =>
            this.Type == BsonExpressionType.Equal ||
            this.Type == BsonExpressionType.Like ||
            this.Type == BsonExpressionType.Between ||
            this.Type == BsonExpressionType.GreaterThan ||
            this.Type == BsonExpressionType.GreaterThanOrEqual ||
            this.Type == BsonExpressionType.LessThan ||
            this.Type == BsonExpressionType.LessThanOrEqual ||
            this.Type == BsonExpressionType.NotEqual ||
            this.Type == BsonExpressionType.In;

        /// <summary>
        /// This expression can be indexed? To index some expression must contains fields (at least 1) and
        /// must use only immutable methods and no parameters
        /// </summary>
        internal bool IsIndexable =>
            this.Fields.Count > 0 &&
            this.IsImmutable == true &&
            this.Parameters.Count == 0;

        /// <summary>
        /// This expression has no dependency of BsonDocument so can be used as user value (when select index)
        /// </summary>
        internal bool IsValue =>
            this.Fields.Count == 0;

        /// <summary>
        /// Indicate when predicate expression uses ALL keywork for filter array items
        /// </summary>
        internal bool IsALL =>
            this.IsPredicate &&
            this.Expression.ToString().Contains("_ALL");

        /// <summary>
        /// Compiled Expression into a function to be executed: func(source[], root, current, parameters)[]
        /// </summary>
        private Func<IEnumerable<BsonDocument>, BsonDocument, BsonValue, Collation, BsonDocument, IEnumerable<BsonValue>> _func;

        /// <summary>
        /// Compiled Expression into a scalar function to be executed: func(source[], root, current, parameters)1
        /// </summary>
        private Func<IEnumerable<BsonDocument>, BsonDocument, BsonValue, Collation, BsonDocument, BsonValue> _funcScalar;

        /// <summary>
        /// Get default field name when need convert simple BsonValue into BsonDocument
        /// </summary>
        internal string DefaultFieldName()
        {
            var name = string.Join("_", this.Fields.Where(x => x != "$"));

            return string.IsNullOrEmpty(name) ? "expr" : name;
        }

        /// <summary>
        /// Only internal ctor (from BsonParserExpression)
        /// </summary>
        internal BsonExpression()
        {
        }

        /// <summary>
        /// Implicit string converter
        /// </summary>
        public static implicit operator String(BsonExpression expr)
        {
            return expr.Source;
        }

        /// <summary>
        /// Implicit string converter
        /// </summary>
        public static implicit operator BsonExpression(String expr)
        {
            return BsonExpression.Create(expr);
        }

        #region Execute Enumerable

        /// <summary>
        /// Execute expression with an empty document (used only for resolve math/functions).
        /// </summary>
        public IEnumerable<BsonValue> Execute(Collation collation = null)
        {
            var root = new BsonDocument();
            var source = new BsonDocument[] { root };

            return this.Execute(source, root, root, collation);
        }

        /// <summary>
        /// Execute expression and returns IEnumerable values
        /// </summary>
        public IEnumerable<BsonValue> Execute(BsonDocument root, Collation collation = null)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            var source = new BsonDocument[] { root };

            return this.Execute(source, root, root, collation);
        }

        /// <summary>
        /// Execute expression and returns IEnumerable values
        /// </summary>
        public IEnumerable<BsonValue> Execute(IEnumerable<BsonDocument> source, Collation collation = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return this.Execute(source, null, null, collation);
        }

        /// <summary>
        /// Execute expression and returns IEnumerable values - returns NULL if no elements
        /// </summary>
        internal IEnumerable<BsonValue> Execute(IEnumerable<BsonDocument> source, BsonDocument root, BsonValue current, Collation collation)
        {
            if (this.IsScalar)
            {
                var value = _funcScalar(source, root, current, collation ?? Collation.Binary, this.Parameters);

                yield return value;
            }
            else
            {
                var values = _func(source, root, current, collation ?? Collation.Binary, this.Parameters);

                foreach (var value in values)
                {
                    yield return value;
                }
            }
        }

        /// <summary>
        /// Execute expression over document to get all index keys. 
        /// Return distinct value (no duplicate key to same document)
        /// </summary>
        internal IEnumerable<BsonValue> GetIndexKeys(BsonDocument doc, Collation collation)
        {
            return this.Execute(doc, collation).Distinct();
        }

        #endregion

        #region ExecuteScalar

        /// <summary>
        /// Execute scalar expression with an blank document and empty source (used only for resolve math/functions).
        /// </summary>
        public BsonValue ExecuteScalar(Collation collation = null)
        {
            var root = new BsonDocument();
            var source = new BsonDocument[] { };

            return this.ExecuteScalar(source, root, root, collation);
        }

        /// <summary>
        /// Execute scalar expression over single document and return a single value (or BsonNull when empty). Throws exception if expression are not scalar expression
        /// </summary>
        public BsonValue ExecuteScalar(BsonDocument root, Collation collation = null)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            var source = new BsonDocument[] { root };

            return this.ExecuteScalar(source, root, root, collation);
        }

        /// <summary>
        /// Execute scalar expression over multiple documents and return a single value (or BsonNull when empty). Throws exception if expression are not scalar expression
        /// </summary>
        public BsonValue ExecuteScalar(IEnumerable<BsonDocument> source, Collation collation = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return this.ExecuteScalar(source, null, null, collation);
        }

        /// <summary>
        /// Execute expression and returns IEnumerable values - returns NULL if no elements
        /// </summary>
        internal BsonValue ExecuteScalar(IEnumerable<BsonDocument> source, BsonDocument root, BsonValue current, Collation collation)
        {
            if (this.IsScalar)
            {
                return _funcScalar(source, root, current, collation ?? Collation.Binary, this.Parameters);
            }
            else
            {
                throw new LiteException(0, $"Expression `{this.Source}` is not a scalar expression and can return more than one result");
            }
        }

        #endregion

        #region Static method

        private static ConcurrentDictionary<string, BsonExpression> _cache = new ConcurrentDictionary<string, BsonExpression>();

        /// <summary>
        /// Parse string and create new instance of BsonExpression - can be cached
        /// </summary>
        public static BsonExpression Create(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) throw new ArgumentNullException(nameof(expression));

            if (!_cache.TryGetValue(expression, out var expr))
            {
                var tokenizer = new Tokenizer(expression);

                expr = Parse(tokenizer, BsonExpressionParserMode.Full, true);

                tokenizer.LookAhead().Expect(TokenType.EOF);

                // if passed string expression are different from formatted expression, try add in cache "unformatted" expression too
                if (expression != expr.Source)
                {
                    _cache.TryAdd(expression, expr);
                }
            }

            // return a copy from cache WITHOUT parameters
            return new BsonExpression
            {
                Expression = expr.Expression,
                IsImmutable = expr.IsImmutable,
                UseSource = expr.UseSource,
                IsScalar = expr.IsScalar,
                Fields = expr.Fields,
                Left = expr.Left,
                Right = expr.Right,
                Source = expr.Source,
                Type = expr.Type,
                _func = expr._func,
                _funcScalar = expr._funcScalar
            };
        }

        /// <summary>
        /// Parse string and create new instance of BsonExpression - can be cached
        /// </summary>
        public static BsonExpression Create(string expression, params BsonValue[] args)
        {
            var expr = Create(expression);

            for(var i = 0; i < args.Length; i++)
            {
                expr.Parameters[i.ToString()] = args[i];
            }

            return expr;
        }

        /// <summary>
        /// Parse string and create new instance of BsonExpression - can be cached
        /// </summary>
        public static BsonExpression Create(string expression, BsonDocument parameters)
        {
            var expr = Create(expression);

            expr.Parameters = parameters;

            return expr;
        }

        /// <summary>
        /// Parse tokenizer and create new instance of BsonExpression - for now, do not use cache
        /// </summary>
        internal static BsonExpression Create(Tokenizer tokenizer, BsonDocument parameters, BsonExpressionParserMode mode)
        {
            if (tokenizer == null) throw new ArgumentNullException(nameof(tokenizer));

            var expr = Parse(tokenizer, mode, true);

            // return a copy from cache using new Parameters
            return new BsonExpression
            {
                Expression = expr.Expression,
                IsImmutable = expr.IsImmutable,
                UseSource = expr.UseSource,
                IsScalar = expr.IsScalar,
                Parameters = parameters ?? new BsonDocument(),
                Fields = expr.Fields,
                Left = expr.Left,
                Right = expr.Right,
                Source = expr.Source,
                Type = expr.Type,
                _func = expr._func,
                _funcScalar = expr._funcScalar
            };
        }

        /// <summary>
        /// Parse and compile string expression and return BsonExpression
        /// </summary>
        internal static BsonExpression Parse(Tokenizer tokenizer, BsonExpressionParserMode mode, bool isRoot)
        {
            if (tokenizer == null) throw new ArgumentNullException(nameof(tokenizer));

            var context = new ExpressionContext();

            var expr =
                mode == BsonExpressionParserMode.Full ? BsonExpressionParser.ParseFullExpression(tokenizer, context, isRoot) :
                mode == BsonExpressionParserMode.Single ? BsonExpressionParser.ParseSingleExpression(tokenizer, context, isRoot) :
                mode == BsonExpressionParserMode.SelectDocument ? BsonExpressionParser.ParseSelectDocumentBuilder(tokenizer, context) :
                BsonExpressionParser.ParseUpdateDocumentBuilder(tokenizer, context);

            // before compile try find in cache if this source already has in cache (already compiled)
            var cached = _cache.GetOrAdd(expr.Source, (s) =>
            {
                // compile linq expression (with left+right expressions)
                Compile(expr, context);
                return expr;
            });

            return cached;
        }

        internal static void Compile(BsonExpression expr, ExpressionContext context)
        {
            // compile linq expression according with return type (scalar or not)
            if (expr.IsScalar)
            {
                var lambda = System.Linq.Expressions.Expression.Lambda<Func<IEnumerable<BsonDocument>, BsonDocument, BsonValue, Collation, BsonDocument, BsonValue>>(expr.Expression, context.Source, context.Root, context.Current, context.Collation, context.Parameters);

                expr._funcScalar = lambda.Compile();
            }
            else
            {
                var lambda = System.Linq.Expressions.Expression.Lambda<Func<IEnumerable<BsonDocument>, BsonDocument, BsonValue, Collation, BsonDocument, IEnumerable<BsonValue>>>(expr.Expression, context.Source, context.Root, context.Current, context.Collation, context.Parameters);

                expr._func = lambda.Compile();
            }

            // compile child expressions (left/right)
            if (expr.Left != null) Compile(expr.Left, context);
            if (expr.Right != null) Compile(expr.Right, context);
        }

        /// <summary>
        /// Get root document $ expression
        /// </summary>
        public static BsonExpression Root = Create("$");

        #endregion

        #region MethodCall quick access

        /// <summary>
        /// Get all registered methods for BsonExpressions
        /// </summary>
        public static IEnumerable<MethodInfo> Methods => _methods.Values;

        /// <summary>
        /// Load all static methods from BsonExpressionMethods class. Use a dictionary using name + parameter count
        /// </summary>
        private static Dictionary<string, MethodInfo> _methods =
            typeof(BsonExpressionMethods).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .ToDictionary(m => m.Name.ToUpper() + "~" + m.GetParameters().Where(p => p.ParameterType != typeof(Collation)).Count());

        /// <summary>
        /// Get expression method with same name and same parameter - return null if not found
        /// </summary>
        internal static MethodInfo GetMethod(string name, int parameterCount)
        {
            var key = name.ToUpper() + "~" + parameterCount;

            return _methods.GetOrDefault(key);
        }

        #endregion

        #region FunctionCall quick access

        /// <summary>
        /// Get all registered functions for BsonExpressions
        /// </summary>
        public static IEnumerable<MethodInfo> Functions => _functions.Values;

        /// <summary>
        /// Load all static methods from BsonExpressionFunctions class. Use a dictionary using name + parameter count
        /// </summary>
        private static Dictionary<string, MethodInfo> _functions =
            typeof(BsonExpressionFunctions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .ToDictionary(m => m.Name.ToUpper() + "~" + m.GetParameters()
            .Skip(5).Count());

        /// <summary>
        /// Get expression function with same name and same parameter - return null if not found
        /// </summary>
        internal static MethodInfo GetFunction(string name, int parameterCount = 0)
        {
            var key = name.ToUpper() + "~" + parameterCount;

            return _functions.GetOrDefault(key);
        }

        #endregion

        public override string ToString()
        {
            return $"`{this.Source}` [{this.Type}]";
        }
    }
}