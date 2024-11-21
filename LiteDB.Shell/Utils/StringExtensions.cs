namespace LiteDB.Shell;

internal static class StringExtensions
{
    public static string TrimToNull(this string str)
    {
        var v = str.Trim();

        return v.Length == 0 ? null : v;
    }
}