namespace LiteDB;

internal partial class BsonExpressionMethods
{
    /// <summary>
    /// </summary>
    public static BsonValue COUNT(BsonValue values)
    {
        if (values is BsonArray array) return array.Count;
        if (values is BsonDocument doc) return doc.Count;

        return 1;
    }

    /// <summary>
    /// </summary>
    public static BsonValue MIN(BsonValue values)
    {
        var min = BsonValue.MaxValue;

        if (values is BsonArray array)
        {
            foreach (var value in array)
            {
                if (value.CompareTo(min) <= 0)
                {
                    min = value;
                }
            }
        }

        return min == BsonValue.MaxValue ? BsonValue.MinValue : min;
    }

    /// <summary>
    /// </summary>
    public static BsonValue MAX(BsonValue values)
    {
        var max = BsonValue.MinValue;

        if (values is BsonArray array)
        {
            foreach (var value in array)
            {
                if (value.CompareTo(max) >= 0)
                {
                    max = value;
                }
            }
        }

        return max == BsonValue.MinValue ? BsonValue.MaxValue : max;
    }

    /// <summary>
    /// </summary>
    public static BsonValue FIRST(BsonValue values)
    {
        if (values is BsonArray array) return array.FirstOrDefault() ?? BsonValue.Null;

        return values;
    }

    /// <summary>
    /// </summary>
    public static BsonValue LAST(BsonValue values)
    {
        if (values is BsonArray array) return array.LastOrDefault() ?? BsonValue.Null;

        return values;
    }

    /// <summary>
    /// Find average value from all values inside array (number only)
    /// </summary>
    public static BsonValue AVG(BsonValue values)
    {
        if (values is not BsonArray array) return values;

        var sumInt = 0;
        var sumLong = 0L;
        var sumDouble = 0d;
        var sumDecimal = 0m;

        foreach (var value in array)
        {
            if (value is BsonInt32 int32) sumInt = unchecked(sumInt + int32);
            else if (value is BsonInt64 int64) sumLong = unchecked(sumLong + int64);
            else if (value is BsonDouble double64) sumDouble = unchecked(sumDouble + double64);
            else if (value is BsonDecimal decimal128) sumDecimal = unchecked(sumDecimal + decimal128);
        }

        if (array.Count > 0)
        {
            if (sumDecimal > 0) return (sumDecimal + Convert.ToDecimal(sumDouble) + sumLong + sumInt) / array.Count;
            if (sumDouble > 0) return (sumDouble + sumLong + sumInt) / array.Count;
            if (sumLong > 0) return (sumLong + sumInt) / array.Count;
            if (sumInt > 0) return (sumInt) / array.Count;
        }

        return 0;
    }

    /// <summary>
    /// Sum all values from array (number only)
    /// </summary>
    public static BsonValue SUM(BsonValue values)
    {
        if (values is not BsonArray array) return values;

        var sumInt = 0;
        var sumLong = 0L;
        var sumDouble = 0d;
        var sumDecimal = 0m;

        foreach (var value in array)
        {
            if (value is BsonInt32 int32) sumInt = unchecked(sumInt + int32);
            else if (value is BsonInt64 int64) sumLong = unchecked(sumLong + int64);
            else if (value is BsonDouble double64) sumDouble = unchecked(sumDouble + double64);
            else if (value is BsonDecimal decimal128) sumDecimal = unchecked(sumDecimal + decimal128);
        }

        if (sumDecimal > 0) return sumDecimal + Convert.ToDecimal(sumDouble) + sumLong + sumInt;
        if (sumDouble > 0) return sumDouble + sumLong + sumInt;
        if (sumLong > 0) return sumLong + sumInt;
        if (sumInt > 0) return sumInt;

        return 0;
    }

    /// <summary>
    /// Return "true" if inner array values contains any result
    /// </summary>
    public static BsonValue ANY(BsonValue values)
    {
        if (values is BsonArray array) return array.Any();

        return false; 
    }
}
