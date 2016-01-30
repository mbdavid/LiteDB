using System;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    internal static class StringExtensions
    {
        public static string ThrowIfEmpty(this string str, string message)
        {
            if(StringExtensions.IsNullOrWhiteSpace(str))
            {
                throw new LiteException(message);
            }

            return str;
        }

        public static bool IsNullOrWhiteSpace(this string str)
        {
            return str == null || str.Trim().Length == 0;
        }
    }
}