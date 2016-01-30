using System;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    internal static class StringExtensions
    {
        public static string ThrowIfEmpty(this string str, string message)
        {
            if(string.IsNullOrWhiteSpace(str))
            {
                throw new LiteException(message);
            }

            return str;
        }
    }
}