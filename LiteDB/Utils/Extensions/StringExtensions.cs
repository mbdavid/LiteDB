using System;

namespace LiteDB
{
    internal static class StringExtensions
    {
        public static bool IsNullOrWhiteSpace(this string str)
        {
            return str == null || str.Trim().Length == 0;
        }

        public static string ThrowIfEmpty(this string str, string message, StringScanner s)
        {
            if (string.IsNullOrEmpty(str) || str.Trim().Length == 0)
            {
                throw LiteException.SyntaxError(s, message);
            }

            return str;
        }

        public static string TrimToNull(this string str)
        {
            var v = str.Trim();

            return v.Length == 0 ? null : v;
        }
    }
}