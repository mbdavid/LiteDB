namespace LiteDB;

internal partial class BsonExpressionMethods
{
    /// <summary>
    /// Apply absolute value (ABS) method in all number values
    /// </summary>
    public static BsonValue ABS(BsonValue value)
    {
        return value.Type switch
        {
            BsonType.Int32 => Math.Abs(value.AsInt32),
            BsonType.Int64 => Math.Abs(value.AsInt64),
            BsonType.Double => Math.Abs(value.AsDouble),
            BsonType.Decimal => Math.Abs(value.AsDecimal),
            _ => BsonValue.Null,
        };
    }

    /// <summary>
    /// Round number method in all number values
    /// </summary>
    public static BsonValue ROUND(BsonValue value, BsonValue digits)
    {
        if (digits.IsNumber)
        {
            switch (value.Type)
            {
                case BsonType.Int32: return value.AsInt32;
                case BsonType.Int64: return value.AsInt64;
                case BsonType.Double: return Math.Round(value.AsDouble, digits.AsInt32);
                case BsonType.Decimal: return Math.Round(value.AsDecimal, digits.AsInt32);
            }

        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Implement POWER (x and y)
    /// </summary>
    public static BsonValue POW(BsonValue x, BsonValue y)
    {
        if (x.IsNumber && y.IsNumber)
        {
            return Math.Pow(x.AsDouble, y.AsDouble);
        }

        return BsonValue.Null;
    }
}
