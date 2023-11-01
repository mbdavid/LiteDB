namespace LiteDB;

/// <summary>
/// Represent a null value constant in Bson object model (BsonNull is a valid value)
/// </summary>
internal class BsonNull : BsonValue
{
    public override BsonType Type => BsonType.Null;

    public override int GetBytesCount() => 0;

    public override int GetHashCode() => this.Type.GetHashCode();

    #region Implement CompareTo

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other is BsonNull) return 0;

        return this.CompareType(other);
    }

    #endregion
}
