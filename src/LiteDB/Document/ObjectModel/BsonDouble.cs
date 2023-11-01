namespace LiteDB;

/// <summary>
/// Represent a double value in Bson object model
/// </summary>
internal class BsonDouble : BsonValue
{
    public static BsonDouble Zero = new(0);
    public static BsonDouble One = new(1);
    public static BsonDouble MinusOne = new(-1);

    public double Value { get; }

    public BsonDouble(double value)
    {
        this.Value = value;
    }

    public override BsonType Type => BsonType.Double;

    public override int GetBytesCount() => sizeof(double);

    public override int GetHashCode() => this.Value.GetHashCode();

    #region Implement CompareTo

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other is BsonDouble otherDouble) return this.Value.CompareTo(otherDouble.Value);
        if (other is BsonInt32 otherInt32) return this.Value.CompareTo(otherInt32.ToDouble());
        if (other is BsonInt64 otherInt64) return this.Value.CompareTo(otherInt64.ToDouble());
        if (other is BsonDecimal otherDecimal) return this.ToDecimal().CompareTo(otherDecimal.Value);

        return this.CompareType(other);
    }

    #endregion

    #region Convert Types

    public override bool ToBoolean() => this.Value != 0;

    public override int ToInt32() => Convert.ToInt32(this.Value);

    public override long ToInt64() => Convert.ToInt64(this.Value);

    public override double ToDouble() => this.Value;

    public override decimal ToDecimal() => Convert.ToDecimal(this.Value);

    #endregion

}
