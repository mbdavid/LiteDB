using System;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    internal static class ExpressionExtensions
    {
        // more dirty as possible: removing ".Select(x => x." sentence
        private static Regex _removeSelect = new Regex(@"\.Select\s*\(\s*\w+\s*=>\s*\w+\.", RegexOptions.Compiled);
        private static Regex _removeList = new Regex(@"\.get_Item\(\d+\)", RegexOptions.Compiled);
        private static Regex _removeArray = new Regex(@"\[\d+\]", RegexOptions.Compiled);

        /// <summary>
        /// Get Path (better ToString) from an Expression.
        /// Support multi levels: x => x.Customer.Address
        /// Support list levels: x => x.Addresses.Select(z => z.StreetName)
        /// </summary>
        public static string GetPath(this Expression expr)
        {
            // quick and dirty solution to support x.Name.SubName
            // http://stackoverflow.com/questions/671968/retrieving-property-name-from-lambda-expression

            var str = expr.ToString(); // gives you: "o => o.Whatever"
            var firstDelim = str.IndexOf('.'); // make sure there is a beginning property indicator; the "." in "o.Whatever" -- this may not be necessary?

            var path = firstDelim < 0 ? str : str.Substring(firstDelim + 1).TrimEnd(')');

            path = _removeList.Replace(path, "");
            path = _removeArray.Replace(path, "");
            path = _removeSelect.Replace(path, ".")
                .Replace(")", "");

            return path;
        }

        public static BinaryExpression Assign(Expression left, Expression right)
        {
            var assign = typeof(Assigner<>).MakeGenericType(left.Type).GetMethod("Assign");

            var assignExpr = Expression.Add(left, right, assign);

            return assignExpr;
        }

        private static class Assigner<T>
        {
            public static T Assign(ref T left, T right)
            {
                return (left = right);
            }
        }
    }
}