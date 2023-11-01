namespace LiteDB;

/// <summary>
/// Represent a String value in Bson object model
/// </summary>
internal class BsonString : BsonValue
{
    public static BsonString Emtpy = new("");

    public static BsonString Id = new("_id");

    public string Value { get; }

    public BsonString(string value)
    {
        this.Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override BsonType Type => BsonType.String;

    public override int GetBytesCount() => Encoding.UTF8.GetByteCount(this.Value);

    public override int GetHashCode() => this.Value.GetHashCode();

    #region Implement CompareTo

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other is BsonString otherString) return collation.Compare(this.Value, otherString.Value);

        return this.CompareType(other);
    }

    #endregion

    #region Implicit Ctor

    public static implicit operator string(BsonString value) => value.Value;

    public static implicit operator BsonString(string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        if (value.Length == 0) return Emtpy;

        return new BsonString(value);
    }

    #endregion

    #region Convert Types

    public override bool ToBoolean() => Convert.ToBoolean(this.Value);

    public override int ToInt32() => Convert.ToInt32(this.Value, CultureInfo.InvariantCulture.NumberFormat);

    public override long ToInt64() => Convert.ToInt64(this.Value, CultureInfo.InvariantCulture.NumberFormat);

    public override double ToDouble() => Convert.ToDouble(this.Value, CultureInfo.InvariantCulture.NumberFormat);

    public override decimal ToDecimal() => Convert.ToDecimal(this.Value, CultureInfo.InvariantCulture.NumberFormat);

    #endregion
}
