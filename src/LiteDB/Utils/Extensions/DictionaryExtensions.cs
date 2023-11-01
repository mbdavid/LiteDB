namespace LiteDB;

internal static class DictionaryExtensions
{
    public static T GetOrDefault<K, T>(this IDictionary<K, T> dict, K key, T defaultValue)
    {
        if (dict.TryGetValue(key, out T result))
        {
            return result;
        }

        return defaultValue;
    }

    public static T GetOrAdd<K, T>(this IDictionary<K, T> dict, K key, Func<K, T> valueFactoy)
    {
        if (dict.TryGetValue(key, out var value) == false)
        {
            value = valueFactoy(key);

            dict.Add(key, value);
        }

        return value;
    }

    /// <summary>
    /// Parse key=value;key1=value1 from a string based on Connection String rules
    /// </summary>
    public static void ParseKeyValue(this IDictionary<string, string> dict, string text)
    {
        var position = 0;

        while(position < text.Length)
        {
            EatWhitespace();
            var key = ReadKey();

            EatWhitespace();
            var value = ReadValue();

            dict[key] = value;
        }

        string ReadKey()
        {
            var sb = new StringBuilder();

            while (position < text.Length)
            {
                var current = text[position];

                if (current == '=')
                {
                    position++;
                    return sb.ToString().Trim();
                }

                sb.Append(current);
                position++;
            }

            return sb.ToString().Trim();
        }

        string ReadValue()
        {
            var sb = new StringBuilder();
            var quote =
                text[position] == '"' ? '"' :
                text[position] == '\'' ? '\'' : ' ';

            if (quote != ' ') position++;

            while (position < text.Length)
            {
                var current = text[position];

                if (quote == ' ')
                {
                    if (current == ';')
                    {
                        position++;
                        return sb.ToString().Trim();
                    }
                }
                else if (quote != ' ' && current == quote)
                {
                    if (text[position - 1] == '\\')
                    {
                        sb.Length--;
                    }
                    else
                    {
                        position++;

                        EatWhitespace();

                        if (position < text.Length && text[position] == ';') position++;

                        return sb.ToString();
                    }
                }

                sb.Append(current);
                position++;
            }

            return sb.ToString().Trim();
        }

        void EatWhitespace()
        {
            while (position < text.Length)
            {
                if(text[position] == ' ' ||
                    text[position] == '\t' ||
                    text[position] == '\f')
                {
                    position++;
                    continue;
                }
                break;
            }
        }
    }
}