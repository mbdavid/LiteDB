namespace LiteDB;

/// <summary>
/// Represent a Boolean value in Bson object model
/// </summary>
internal class BsonBoolean : BsonValue
{
    public static readonly BsonBoolean True = new (true);
    public static readonly BsonBoolean False = new (false);

    public bool Value { get; }

    public BsonBoolean(bool value)
    {
        this.Value = value;
    }

    public override BsonType Type => BsonType.Boolean;

    public override int GetBytesCount() => 0; // use 2 different BsonTypeCode for true|false

    public override int GetHashCode() => this.Value.GetHashCode();

    #region Implement CompareTo

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other is BsonBoolean otherBoolean) return this.Value.CompareTo(otherBoolean.Value);

        return this.CompareType(other);
    }

    #endregion

    #region Convert Types

    public override bool ToBoolean() => this.Value;

    public override int ToInt32() => this.Value ? 1 : 0;

    public override long ToInt64() => this.Value ? 1 : 0;

    public override double ToDouble() => this.Value ? 1 : 0;

    public override decimal ToDecimal() => this.Value ? 1 : 0;

    #endregion
}
