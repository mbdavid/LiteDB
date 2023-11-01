namespace LiteDB;

/// <summary>
/// Represent a DateTime value in Bson object model
/// </summary>
internal class BsonDateTime : BsonValue
{
    public static readonly DateTime UnixEpoch = new (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public DateTime Value { get; }

    public BsonDateTime(DateTime value)
    {
        this.Value = value;
    }

    public override BsonType Type => BsonType.DateTime;

    public override int GetBytesCount() => 8;

    public override int GetHashCode() => this.Value.GetHashCode();

    #region Implement CompareTo

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other is BsonDateTime otherDateTime) return this.Value.CompareTo(otherDateTime.Value);

        return this.CompareType(other);
    }

    #endregion
}
