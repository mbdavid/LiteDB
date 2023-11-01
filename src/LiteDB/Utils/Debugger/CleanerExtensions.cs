namespace LiteDB;

/// <summary>
/// A quick and dirty cleaner names from LINQ expression (debug use only)
/// </summary>
internal static class CleanerExtensions
{
    /// <summary>
    /// Clean TypeName (remove async strings)
    /// </summary>
    public static string CleanName(this Type type)
    {
        var str = type.Name;

        str = Regex.Replace(str, @"^<(.*)>.*", "$1");

        return str;
    }

    /// <summary>
    /// Clean a LINQ expression
    /// </summary>
    public static string Clean(this Expression e)
    {
        var str = e.ToString();

        str = Regex.Replace(str, @"value\(.*?\)\.", "");
        str = Regex.Replace(str, @"^value\(.*\.(.*)\)$", "$1");
        str = Regex.Replace(str, @" AndAlso ", " && ");
        str = Regex.Replace(str, @" OrElse ", " || ");

        str = Regex.Replace(str, @"^\((.*)\)$", "$1");

        return str;
    }
}
