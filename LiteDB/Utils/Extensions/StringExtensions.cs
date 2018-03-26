using System;
using System.Collections.Generic;
using System.Text;

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

        /// <summary>
        /// Implement SqlLike in C# string 
        /// https://stackoverflow.com/a/8583383/3286260
        /// </summary>
        public static bool SqlLike(this string str, string pattern)
        {
            //TODO remove ToUpper in SqlLike (must be tested outside)

            var isMatch = true;
            var isWildCardOn = false;
            var isCharWildCardOn = false;
            var isCharSetOn = false;
            var isNotCharSetOn = false;
            var endOfPattern = false;
            var lastWildCard = -1;
            var patternIndex = 0;
            var set = new List<char>();
            var p = '\0';

            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];

                endOfPattern = (patternIndex >= pattern.Length);

                if (!endOfPattern)
                {
                    p = pattern[patternIndex];

                    if (!isWildCardOn && p == '%')
                    {
                        lastWildCard = patternIndex;
                        isWildCardOn = true;

                        while (patternIndex < pattern.Length && pattern[patternIndex] == '%')
                        {
                            patternIndex++;
                        }

                        if (patternIndex >= pattern.Length)
                        {
                            p = '\0';
                        }
                        else
                        {
                            p = pattern[patternIndex];
                        }
                    }
                    else if (p == '_')
                    {
                        isCharWildCardOn = true;
                        patternIndex++;
                    }
                    else if (p == '[')
                    {
                        if (pattern[++patternIndex] == '^')
                        {
                            isNotCharSetOn = true;
                            patternIndex++;
                        }
                        else
                        {
                            isCharSetOn = true;
                        }

                        set.Clear();

                        if (pattern[patternIndex + 1] == '-' && pattern[patternIndex + 3] == ']')
                        {
                            var start = char.ToUpper(pattern[patternIndex]);
                            patternIndex += 2;
                            var end = char.ToUpper(pattern[patternIndex]);

                            if (start <= end)
                            {
                                for (var ci = start; ci <= end; ci++)
                                {
                                    set.Add(ci);
                                }
                            }

                            patternIndex++;
                        }

                        while (patternIndex < pattern.Length && pattern[patternIndex] != ']')
                        {
                            set.Add(pattern[patternIndex]);
                            patternIndex++;
                        }

                        patternIndex++;
                    }
                }

                if (isWildCardOn)
                {
                    if (char.ToUpper(c) == char.ToUpper(p))
                    {
                        isWildCardOn = false;
                        patternIndex++;
                    }
                }
                else if (isCharWildCardOn)
                {
                    isCharWildCardOn = false;
                }
                else if (isCharSetOn || isNotCharSetOn)
                {
                    var charMatch = (set.Contains(char.ToUpper(c)));

                    if ((isNotCharSetOn && charMatch) || (isCharSetOn && !charMatch))
                    {
                        if (lastWildCard >= 0)
                        {
                            patternIndex = lastWildCard;
                        }
                        else
                        {
                            isMatch = false;
                            break;
                        }
                    }

                    isNotCharSetOn = isCharSetOn = false;
                }
                else
                {
                    if (char.ToUpper(c) == char.ToUpper(p))
                    {
                        patternIndex++;
                    }
                    else
                    {
                        if (lastWildCard >= 0)
                        {
                            patternIndex = lastWildCard;
                        }
                        else
                        {
                            isMatch = false;
                            break;
                        }
                    }
                }
            }

            endOfPattern = (patternIndex >= pattern.Length);

            if (isMatch && !endOfPattern)
            {
                var isOnlyWildCards = true;

                for (var i = patternIndex; i < pattern.Length; i++)
                {
                    if (pattern[i] != '%')
                    {
                        isOnlyWildCards = false;
                        break;
                    }
                }

                if (isOnlyWildCards) endOfPattern = true;
            }

            return isMatch && endOfPattern;
        }

        /// <summary>
        /// Get first string before any %, _, [... used to index startswith - out if has more string pattern after found wildcard
        /// </summary>
        public static string SqlLikeStartsWith(this string str, out bool hasMore)
        {
            var sb = new StringBuilder();

            hasMore = true;

            for (int i = 0; i < str.Length; i++)
            {
                var c = str[i];

                if (c == '%' || c == '[' || c == '_')
                {
                    if (i == str.Length - 1) hasMore = false;

                    break;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}