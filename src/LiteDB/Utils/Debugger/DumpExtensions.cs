namespace LiteDB;

internal static class Dump
{
    /// <summary>
    /// Implement a simple object deserialization do better reader in debug mode
    /// </summary>
    public static string Object(object obj)
    {
        var type = obj.GetType();
        var sb = new StringBuilder();

        var isEmpty = type.GetProperties().FirstOrDefault(x => x.Name == "IsEmpty");
        if (isEmpty is not null && (bool)isEmpty.GetValue(obj) == true) return "<EMPTY>";

        var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty)
            .OrderBy(x => x.MemberType == MemberTypes.Field ? 0 : 1)
            .ToArray();

        foreach (var member in members)
        {
            if (member is FieldInfo f)
            {
                if (f.Name.EndsWith("__BackingField") || f.Name.EndsWith("__Field")) continue;

                var value = GetStringValue(f.GetValue(obj));

                if (value.Length > 0)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append($"{f.Name}: {value}");
                }
            }
            else if (member is PropertyInfo p)
            {
                var value = GetStringValue(p.GetValue(obj));

                if (p.Name == "IsEmpty") continue;

                if (value.Length > 0)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append($"{p.Name}: {value}");
                }

            }
        }

        return sb.Length > 0 ? $"{{ {sb} }}" : "{}";
    }

    /// <summary>
    /// Implement a simple array deserialization do better reader in debug mode
    /// </summary>
    public static string Array(IEnumerable enumerable)
    {
        var sb = new StringBuilder();

        foreach(var item in enumerable)
        {
            var value = GetStringValue(item);

            if (value.Length > 0)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(value);
            }
        }

        return sb.Length > 0 ? $"[ {sb} ]" : "[]";
    }

    private static string GetStringValue(object? value)
    {
        if (value is null)
        {
            return "null";
        }
        else
        {
            var type = value.GetType();

            if (Reflection.IsCollection(type) || Reflection.IsDictionary(type))
            {
                var count = type.GetProperties().FirstOrDefault(x => x.Name == "Count").GetValue(value, null);
                return $"[{count}]";
            }
            else if (Reflection.IsSimpleType(type))
            {
                return value.ToString();
            }
            else
            {
                var toString = Reflection.IsOverride(type.GetMethods().FirstOrDefault(x => x.Name == "ToString" && x.GetParameters().Length == 0));
                var isLiteDB = type.Namespace.StartsWith("LiteDB");
                var isEmpty = type.GetProperties().FirstOrDefault(x => x.Name == "IsEmpty");

                if (isLiteDB && isEmpty is not null && (bool)isEmpty.GetValue(value) == true) return "<EMPTY>";

                if (toString && isLiteDB)
                {
                    return value.ToString();
                }
            }
        }

        return "";
    }

    [Obsolete]
    public static string PageID(int id)
    {
        return id == int.MaxValue ? "<EMPTY>" : id.ToString().PadLeft(4, '0');
    }

    public static string PageID(uint id)
    {
        return id == uint.MaxValue ? "<EMPTY>" : id.ToString().PadLeft(4, '0');
    }

    public static string ExtendValue(uint value)
    {
        var str = Convert.ToString(value, 2).PadLeft(32, '0');

        return str[..8] + '-' +
            str[8..11] + '-' +
            str[11..14] + '-' +
            str[14..17] + '-' +
            str[17..20] + '-' +
            str[20..23] + '-' +
            str[23..26] + '-' +
            str[26..29] + '-' +
            str[29..] + " (" +
            label(str[8..11]) + label(str[11..14]) + label(str[14..17]) + label(str[17..20]) +
            label(str[20..23]) + label(str[23..26]) + label(str[26..29]) + label(str[29..]) + ")";

        static string label(string str)
        {
            return str switch
            {
                "000" => "e", // empty
                "001" => "d", // data
                "010" => "i", // index
                "111" => "f", // full
                _ => "*"
            };
        }
    }
}
