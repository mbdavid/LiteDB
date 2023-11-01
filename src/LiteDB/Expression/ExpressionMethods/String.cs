namespace LiteDB;

internal partial class BsonExpressionMethods
{
    /// <summary>
    /// Return lower case from string value
    /// </summary>
    public static BsonValue LOWER(BsonValue value)
    {
        if (value.IsString)
        {
            return value.AsString.ToLowerInvariant();
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Return UPPER case from string value
    /// </summary>
    public static BsonValue UPPER(BsonValue value)
    {
        if (value.IsString)
        {
            return value.AsString.ToUpperInvariant();
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Apply Left TRIM (start) from string value
    /// </summary>
    public static BsonValue LTRIM(BsonValue value)
    {
        if (value.IsString)
        {
            return value.AsString.TrimStart();
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Apply Right TRIM (end) from string value
    /// </summary>
    public static BsonValue RTRIM(BsonValue value)
    {
        if (value.IsString)
        {
            return value.AsString.TrimEnd();
        }

        return BsonValue.Null;

    }

    /// <summary>
    /// Apply TRIM from string value
    /// </summary>
    public static BsonValue TRIM(BsonValue value)
    {
        if (value.IsString)
        {
            return value.AsString.Trim();
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Reports the zero-based index of the first occurrence of the specified string in this instance
    /// </summary>
    public static BsonValue INDEXOF(BsonValue value, BsonValue search)
    {
        if (value.IsString && search.IsString)
        {
            return value.AsString.IndexOf(search.AsString);
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Reports the zero-based index of the first occurrence of the specified string in this instance
    /// </summary>
    public static BsonValue INDEXOF(BsonValue value, BsonValue search, BsonValue startIndex)
    {
        if (value.IsString && search.IsString && startIndex.IsNumber)
        {
            return value.AsString.IndexOf(search.AsString, startIndex.ToInt32());
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Returns substring from string value using index and length (zero-based)
    /// </summary>
    public static BsonValue SUBSTRING(BsonValue value, BsonValue startIndex)
    {
        if (value.IsString && startIndex.IsNumber)
        {
            return value.AsString.Substring(startIndex.ToInt32());
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Returns substring from string value using index and length (zero-based)
    /// </summary>
    public static BsonValue SUBSTRING(BsonValue value, BsonValue startIndex, BsonValue length)
    {
        if (value.IsString && startIndex.IsNumber && length.IsNumber)
        {
            return value.AsString.Substring(startIndex.ToInt32(), length.ToInt32());
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Returns replaced string changing oldValue with newValue
    /// </summary>
    public static BsonValue REPLACE(BsonValue value, BsonValue oldValue, BsonValue newValue)
    {
        if (value.IsString && oldValue.IsString && newValue.IsString)
        {
            return value.AsString.Replace(oldValue.AsString, newValue.AsString);
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Return value string with left padding
    /// </summary>
    public static BsonValue LPAD(BsonValue value, BsonValue totalWidth, BsonValue paddingChar)
    {
        if (value.IsString && totalWidth.IsNumber && paddingChar.IsString)
        {
            var c = paddingChar.AsString;

            if (string.IsNullOrEmpty(c)) throw new ArgumentOutOfRangeException(nameof(paddingChar));

            return value.AsString.PadLeft(totalWidth.ToInt32(), c[0]);
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Return value string with right padding
    /// </summary>
    public static BsonValue RPAD(BsonValue value, BsonValue totalWidth, BsonValue paddingChar)
    {
        if (value.IsString && totalWidth.IsNumber && paddingChar.IsString)
        {
            var c = paddingChar.AsString;

            if (string.IsNullOrEmpty(c)) throw new ArgumentOutOfRangeException(nameof(paddingChar));

            return value.AsString.PadRight(totalWidth.ToInt32(), c[0]);
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Slit value string based on separator 
    /// </summary>
    public static BsonValue SPLIT(BsonValue value, BsonValue separator)
    {
        IEnumerable<BsonValue> source()
        {
            if (value.IsString && separator.IsString)
            {
                var values = value.AsString.Split(new string[] { separator.AsString }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var str in values)
                {
                    yield return str;
                }
            }
        }

        return new BsonArray(source());
    }

    /// <summary>
    /// Slit value string based on regular expression pattern
    /// </summary>
    public static BsonValue SPLIT(BsonValue value, BsonValue pattern, BsonValue useRegex)
    {
        IEnumerable<BsonValue> source()
        {
            if (value.IsString && pattern.IsString)
            {
                var values = Regex.Split(value.AsString, pattern.AsString, RegexOptions.Compiled);

                foreach (var str in values)
                {
                    yield return str;
                }
            }
        }

        if (useRegex.IsBoolean && useRegex.AsBoolean)
        {
            return new BsonArray(source());
        }
        else
        {
            return SPLIT(value, pattern);
        }
    }

    /// <summary>
    /// Return format value string using format definition (same as String.Format("{0:~}", values)).
    /// </summary>
    public static BsonValue FORMAT(BsonValue value, BsonValue format)
    {
        if (format.IsString)
        {
            var fmt = "{0:" + format.AsString + "}";

            switch (value.Type)
            {
                case BsonType.String: return string.Format(fmt, value.AsString);
                case BsonType.Int32: return string.Format(fmt, value.AsInt32);
                case BsonType.Int64: return string.Format(fmt, value.AsInt64);
                case BsonType.Double: return string.Format(fmt, value.AsDouble);
                case BsonType.Decimal: return string.Format(fmt, value.AsDecimal);
                case BsonType.DateTime: return string.Format(fmt, value.AsDateTime);
                case BsonType.Guid: return string.Format(fmt, value.AsGuid);
            }
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Join all values into a single string with ',' separator.
    /// </summary>
    public static BsonValue JOIN(BsonValue array)
    {
        return JOIN(array, "");
    }

    /// <summary>
    /// Join all values into a single string with a string separator
    /// </summary>
    public static BsonValue JOIN(BsonValue array, BsonValue separator)
    {
        if (!array.IsArray || !separator.IsString) return BsonValue.Null;

        return string.Join(
            separator.AsString,
            array.AsArray.Select(x => STRING(x).AsString).ToArray()
        );
    }

    /// <summary>
    /// Test if value is match with regular expression pattern
    /// </summary>
    public static BsonValue IS_MATCH(BsonValue value, BsonValue pattern)
    {
        if (value.IsString == false || pattern.IsString == false) return false;

        return Regex.IsMatch(value.AsString, pattern.AsString);
    }

    /// <summary>
    /// Apply regular expression pattern over value to get group data. Return null if not found
    /// </summary>
    public static BsonValue MATCH(BsonValue value, BsonValue pattern, BsonValue group)
    {
        if (value.IsString == false || pattern.IsString == false) return BsonValue.Null;

        var match = Regex.Match(value.AsString, pattern.AsString);

        if (match.Success == false) return BsonValue.Null;

        if (group.IsNumber)
        {
            return match.Groups[group.AsInt32].Value;
        }
        else if (group.IsString)
        {
            return match.Groups[group.AsString].Value;
        }
        else
        {
            return BsonValue.Null;
        }
    }
}