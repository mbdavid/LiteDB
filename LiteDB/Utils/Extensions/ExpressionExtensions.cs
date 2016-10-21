using System;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    internal static class ExpressionExtensions
    {
        /// <summary>
        /// Get Path (better ToString) from an Expression.
        /// Support multi levels: x => x.Customer.Address
        /// Support list levels: x => x.Addresses(z => z.StreetName)
        /// </summary>
        public static string GetPath(this Expression expr)
        {
            // quick and dirty solution to support x.Name.SubName
            // http://stackoverflow.com/questions/671968/retrieving-property-name-from-lambda-expression

            var str = expr.ToString(); // gives you: "o => o.Whatever"
            var firstDelim = str.IndexOf('.'); // make sure there is a beginning property indicator; the "." in "o.Whatever" -- this may not be necessary?

            var path = firstDelim < 0 ? str : str.Substring(firstDelim + 1).TrimEnd(')');

            return path;
        }
    }
}