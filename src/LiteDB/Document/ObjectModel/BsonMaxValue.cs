namespace LiteDB;

/// <summary>
/// Represent a max value constant in Bson object model
/// </summary>
internal class BsonMaxValue : BsonValue
{
    public override BsonType Type => BsonType.MaxValue;

    public override int GetBytesCount() => 0;

    public override int GetHashCode() => this.Type.GetHashCode();

    #region Implement CompareTo

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other is BsonMaxValue) return 0;

        return 1; // all types are lower than MaxValue
    }

    #endregion
}
