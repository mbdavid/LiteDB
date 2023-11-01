namespace LiteDB;

internal class JsonWriterStatic
{
    private readonly static IFormatProvider _numberFormat = CultureInfo.InvariantCulture.NumberFormat;

    /// <summary>
    /// Serialize value into string build
    /// </summary>
    public static string Serialize(BsonValue value)
    {
        var sb = StringBuilderCache.Acquire();

        Serialize(sb, value);

        return StringBuilderCache.Release(sb);
    }

    /// <summary>
    /// Serialize value into string build
    /// </summary>
    public static void Serialize(StringBuilder writer, BsonValue value)
    {
        // use direct cast to better performance
        switch (value.Type)
        {
            case BsonType.Null:
                writer.Append("null");
                break;

            case BsonType.Array:
                WriteArray(writer, value.AsArray);
                break;

            case BsonType.Document:
                WriteObject(writer, value.AsDocument);
                break;

            case BsonType.Boolean:
                writer.Append(value.AsBoolean ? "true" : "false");
                break;

            case BsonType.String:
                WriteString(writer, value.AsString);
                break;

            case BsonType.Int32:
                writer.Append(value.AsInt32.ToString(_numberFormat));
                break;

            case BsonType.Double:
                writer.Append(value.AsDouble.ToString("0.0########", _numberFormat));
                break;

            case BsonType.Binary:
                var bytes = value.AsBinary;
                WriteExtendDataType(writer, "$binary", Convert.ToBase64String(bytes, 0, bytes.Length));
                break;

            case BsonType.ObjectId:
                WriteExtendDataType(writer, "$oid", value.AsObjectId.ToString());
                break;

            case BsonType.Guid:
                WriteExtendDataType(writer, "$guid", value.AsGuid.ToString());
                break;

            case BsonType.DateTime:
                WriteExtendDataType(writer, "$date", value.AsDateTime.ToUniversalTime().ToString("o"));
                break;

            case BsonType.Int64:
                WriteExtendDataType(writer, "$numberLong", value.AsInt64.ToString(_numberFormat));
                break;

            case BsonType.Decimal:
                WriteExtendDataType(writer, "$numberDecimal", value.AsDecimal.ToString(_numberFormat));
                break;

            case BsonType.MinValue:
                WriteExtendDataType(writer, "$minValue", "1");
                break;

            case BsonType.MaxValue:
                WriteExtendDataType(writer, "$maxValue", "1");
                break;
        }
    }

    private static void WriteObject(StringBuilder writer, BsonDocument obj)
    {
        writer.Append('{');

        var index = 0;

        foreach (var el in obj.GetElements())
        {
            writer.Append(++index > 1 ? ',' : "");

            writer.Append('\"');
            writer.Append(el.Key);
            writer.Append('\"');
            writer.Append(':');

            Serialize(writer, el.Value);
        }

        writer.Append('}');
    }

    private static void WriteArray(StringBuilder writer, BsonArray arr)
    {
        writer.Append('[');

        for (var i = 0; i < arr.Count; i++)
        {
            var item = arr[i];

            if (i > 0) writer.Append(',');

            Serialize(writer, item);
        }

        writer.Append(']');
    }

    private static void WriteString(StringBuilder writer, string s)
    {
        writer.Append('\"');
        int l = s.Length;
        for (var index = 0; index < l; index++)
        {
            var c = s[index];
            switch (c)
            {
                case '\"':
                    writer.Append("\\\"");
                    break;

                case '\\':
                    writer.Append("\\\\");
                    break;

                case '\b':
                    writer.Append("\\b");
                    break;

                case '\f':
                    writer.Append("\\f");
                    break;

                case '\n':
                    writer.Append("\\n");
                    break;

                case '\r':
                    writer.Append("\\r");
                    break;

                case '\t':
                    writer.Append("\\t");
                    break;

                default:
                    switch (CharUnicodeInfo.GetUnicodeCategory(c))
                    {
                        case UnicodeCategory.UppercaseLetter:
                        case UnicodeCategory.LowercaseLetter:
                        case UnicodeCategory.TitlecaseLetter:
                        case UnicodeCategory.OtherLetter:
                        case UnicodeCategory.DecimalDigitNumber:
                        case UnicodeCategory.LetterNumber:
                        case UnicodeCategory.OtherNumber:
                        case UnicodeCategory.SpaceSeparator:
                        case UnicodeCategory.ConnectorPunctuation:
                        case UnicodeCategory.DashPunctuation:
                        case UnicodeCategory.OpenPunctuation:
                        case UnicodeCategory.ClosePunctuation:
                        case UnicodeCategory.InitialQuotePunctuation:
                        case UnicodeCategory.FinalQuotePunctuation:
                        case UnicodeCategory.OtherPunctuation:
                        case UnicodeCategory.MathSymbol:
                        case UnicodeCategory.CurrencySymbol:
                        case UnicodeCategory.ModifierSymbol:
                        case UnicodeCategory.OtherSymbol:
                            writer.Append(c);
                            break;
                        default:
                            writer.Append("\\u");
                            writer.Append(((int)c).ToString("x04"));
                            break;
                    }
                    break;
            }
        }
        writer.Append('\"');
    }

    private static void WriteExtendDataType(StringBuilder writer, string type, string value)
    {
        // format: { "$type": "string-value" }
        // no string.Format to better performance
        writer.Append('{');
        writer.Append('\"');
        writer.Append(type);
        writer.Append('\"');
        writer.Append(':');
        writer.Append('\"');
        writer.Append(value);
        writer.Append('\"');
        writer.Append('}');
    }
}
