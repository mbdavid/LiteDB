using System;

namespace LiteDB.Shell
{
    internal static class StringExtensions
    {
        public static string ThrowIfEmpty(this string str, string message)
        {
            if(string.IsNullOrWhiteSpace(str))
            {
                throw new ArgumentException(message);
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