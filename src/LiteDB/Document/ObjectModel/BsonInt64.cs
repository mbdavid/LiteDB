namespace LiteDB;

/// <summary>
/// Represent an Int64 value in Bson object model
/// </summary>
internal class BsonInt64 : BsonValue
{
    public static BsonInt64 Zero = new(0);
    public static BsonInt64 One = new(1);
    public static BsonInt64 MinusOne = new(-1);

    public long Value { get; }

    public BsonInt64(long value)
    {
        this.Value = value;
    }

    public override BsonType Type => BsonType.Int64;

    public override int GetBytesCount() => sizeof(long);

    public override int GetHashCode() => this.Value.GetHashCode();

    #region Implement CompareTo

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other is BsonInt64 otherInt64) return this.Value.CompareTo(otherInt64.Value);
        if (other is BsonInt32 otherInt32) return this.Value.CompareTo(otherInt32.ToInt64());
        if (other is BsonDouble otherDouble) return this.Value.CompareTo(otherDouble.Value);
        if (other is BsonDecimal otherDecimal) return this.Value.CompareTo(otherDecimal.Value);

        return this.CompareType(other);
    }

    #endregion

    #region Convert Types

    public override bool ToBoolean() => this.Value != 0;

    public override int ToInt32() => Convert.ToInt32(this.Value);

    public override long ToInt64() => this.Value;

    public override double ToDouble() => this.Value;

    public override decimal ToDecimal() => this.Value;

    #endregion
}
