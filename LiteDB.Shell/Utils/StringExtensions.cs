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
    }
}