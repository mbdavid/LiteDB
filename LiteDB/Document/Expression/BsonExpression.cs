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
    public class BsonExpression
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
        /// If true, this expression do not change if same document/paramter are passed (only few methods change - like NOW())
        /// </summary>
        internal bool IsImmutable { get; set; }

        /// <summary>
        /// If true, indicate that it's possible execute this expression without document - always same result
        /// </summary>
        internal bool IsConstant { get; set; }

        /// <summary>
        /// In conditional/or/and expressions, indicate Left side
        /// </summary>
        internal BsonExpression Left { get; set; }

        /// <summary>
        /// In conditional/or/and expressions, indicate Rigth side
        /// </summary>
        internal BsonExpression Right { get; set; }

        /// <summary>
        /// Get transformed LINQ expression
        /// </summary>
        internal Expression Expression { get; set; }

        /// <summary>
        /// Indicate that expression are binary conditional expression (=, >, ...)
        /// </summary>
        internal bool IsConditional =>
            this.Type == BsonExpressionType.GreaterThan ||
            this.Type == BsonExpressionType.GreaterThanOrEqual ||
            this.Type == BsonExpressionType.LessThan ||
            this.Type == BsonExpressionType.LessThanOrEqual ||
            this.Type == BsonExpressionType.NotEqual ||
            this.Type == BsonExpressionType.Equal;

        /// <summary>
        /// Compiled Expression into a function to be executed
        /// </summary>
        private Func<BsonDocument, BsonValue, IEnumerable<BsonValue>> _func;

        /// <summary>
        /// Only internal ctor (from BsonParserExpression)
        /// </summary>
        internal BsonExpression()
        {
        }

        #region Compile and execute

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
            if (this.Type == BsonExpressionType.Empty)
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

        #region Static method

        private static ConcurrentDictionary<string, BsonExpression> _cache = new ConcurrentDictionary<string, BsonExpression>();

        /// <summary>
        /// Create an empty expression - Return same doc (similar to "$")
        /// </summary>
        public static BsonExpression Empty => new BsonExpression { Type = BsonExpressionType.Empty };

        /// <summary>
        /// Parse string and create new instance of BsonExpression - can be cached
        /// </summary>
        public static BsonExpression Create(string expression)
        {
            var expr = _cache.GetOrAdd(expression, (k) => Parse(new StringScanner(expression), false).Single());

            return expr;
        }

        /// <summary>
        /// Parse and compile string expression and return a list of expression - if onlyTerm = true, return a list of all expressions without any AND operator.
        /// </summary>
        public static List<BsonExpression> Parse(StringScanner s, bool onlyTerms)
        {
            var root = Expression.Parameter(typeof(BsonDocument), "root");
            var current = Expression.Parameter(typeof(BsonValue), "current");

            var exprList = BsonExpressionParser.ParseFullExpression(s, root, current, true, onlyTerms);

            foreach(var expr in exprList)
            {
                var lambda = System.Linq.Expressions.Expression.Lambda<Func<BsonDocument, BsonValue, IEnumerable<BsonValue>>>(expr.Expression, root, current);

                expr._func = lambda.Compile();

            }

            return exprList;
        }

        #endregion

        public override string ToString()
        {
            return $"{this.Source} ({this.Type})";
        }
    }
}