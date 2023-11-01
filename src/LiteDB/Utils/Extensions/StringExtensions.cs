namespace LiteDB;

internal static class StringExtensions
{
    /// <summary>
    /// Test if string is simple word pattern ([a-Z$_]+)
    /// </summary>
    public static bool IsWord(this string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return false;

        for (var i = 0; i < str.Length; i++)
        {
            if (!Tokenizer.IsWordChar(str[i], i == 0)) return false;
        }

        return true;
    }

    /// <summary>
    /// Same pattern from WORD but with no $ at begin ([a-Z_][a-Z$_]*)
    /// </summary>
    public static bool IsIdentifier(this string str, int maxLength = 0)
    {
        if (string.IsNullOrWhiteSpace(str)) return false;

        if (maxLength > 0 && str.Length > maxLength) return false;

        for (var i = 0; i < str.Length; i++)
        {
            if (!Tokenizer.IsWordChar(str[i], i == 0)) return false;
        }

        return true;
    }

    /// <summary>
    /// Equals with igore case
    /// </summary>
    public static bool Eq(this string str, string other)
    {
        return String.Equals(str, other, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Implement SqlLike in C# string - based on
    /// https://stackoverflow.com/a/8583383/3286260
    /// I removed support for [ and ] to avoid missing close brackets
    /// </summary>
    public static bool SqlLike(this string str, string pattern, Collation collation)
    {
        var isMatch = true;
        var isWildCardOn = false;
        var isCharWildCardOn = false;
        var isCharSetOn = false;
        var isNotCharSetOn = false;
        var lastWildCard = -1;
        var patternIndex = 0;
        var p = '\0';
        bool endOfPattern;

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
                //var charMatch = (set.Contains(char.ToUpper(c))); // -- always "false" - remove [abc] support
                //if ((isNotCharSetOn && charMatch) || (isCharSetOn && !charMatch))

                if (isCharSetOn)
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
                if (collation.Equals(c, p))
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
    /// Get first string before any `%` or `_` ... used to index startswith - out if has more string pattern after found wildcard
    /// </summary>
    public static string SqlLikeStartsWith(this string str, out bool hasMore)
    {
        var i = 0;
        var len = str.Length;
        var c = '\0';

        while (i < len)
        {
            c = str[i];

            if (c == '%' || c == '_')
            {
                break;
            }

            i++;
        }

        hasMore = !(i == len || i == len - 1);

        return str[..i];
    }
}