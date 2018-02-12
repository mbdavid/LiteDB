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
    public partial class BsonExpression
    {
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
            //["startswith"] = typeof(ExpressionOperators).GetMethod("STARTSWITH"),
            //["endswith"] = typeof(ExpressionOperators).GetMethod("ENDSWITH"),
            //["between"] = typeof(ExpressionOperators).GetMethod("BETWEEN"),
            ["&&"] = typeof(ExpressionOperators).GetMethod("AND"),
            ["||"] = typeof(ExpressionOperators).GetMethod("OR")
        };

        private static MethodInfo _rootMethod = typeof(ExpressionAccess).GetMethod("ROOT");
        private static MethodInfo _memberMethod = typeof(ExpressionAccess).GetMethod("MEMBER");
        private static MethodInfo _arrayMethod = typeof(ExpressionAccess).GetMethod("ARRAY");

        #region Regular expression definitions

        /// <summary>
        /// + - * / = > ...
        /// </summary>
        private static Regex RE_OPERATORS = new Regex(@"^\s*(\+|\-|\*|\/|%|=|!=|>=|>|<=|<|&&|\|\|)\s*", RegexOptions.Compiled);
        private static Regex RE_SIMPLE_FIELD = new Regex(@"^[$\w]+$", RegexOptions.Compiled);

        #endregion

        #region Expression reader/creator

        private static ConcurrentDictionary<string, BsonExpression> _cache = new ConcurrentDictionary<string, BsonExpression>();

        /// <summary>
        /// Parse string and create new instance of BsonExpression - can be cached
        /// </summary>
        public static BsonExpression Create(string expression)
        {
            var expr = _cache.GetOrAdd(expression, (k) => new BsonExpression(k));

            return expr;
        }

        /// <summary>
        /// Create an empty expression - Return same doc (similar to "$")
        /// </summary>
        public static BsonExpression Empty => new BsonExpression();

        /// <summary>
        /// Extract expression or a path from a StringScanner. If required = true, throw error if is not a valid expression. If required = false, returns null for not valid expression and back Index in StringScanner to original position
        /// </summary>
        internal static BsonExpression ReadExpression(StringScanner s, bool required)
        {
            var start = s.Index;

            try
            {
                return new BsonExpression(s);
            }
            catch (LiteException ex) when (required == false && ex.ErrorCode == LiteException.SYNTAX_ERROR)
            {
                s.Index = start;
                return null;
            }
        }

        #endregion


        #region Get Method

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
    }
}