using System;

namespace LiteDB
{
    public static class ContainsExt
    {
        public static bool Contains(this string content, string value, StringComparison comp)
        {
            return content.IndexOf(value, comp) >= 0;
        }
    }
}
