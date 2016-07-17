using System;

namespace LiteDB
{
    internal static class StringExtensions
    {
        public static string ThrowIfEmpty(this string str, string message)
        {
            if (str.IsNullOrWhiteSpace())
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