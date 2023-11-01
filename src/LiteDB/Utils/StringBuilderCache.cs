namespace LiteDB;

internal static class StringBuilderCache
{
    private static StringBuilder? _cache;

    public static StringBuilder Acquire()
    {
        var sb = _cache;

        if (sb is not null)
        {
            _cache = null;
            sb.Clear();
            return sb;
        }

        return new StringBuilder();
    }

    public static string Release(StringBuilder sb)
    {
        var result = sb.ToString();

        _cache = sb;

        return result;
    }
}