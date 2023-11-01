namespace LiteDB;

/// <summary>
/// Represent a min value constant in Bson object model
/// </summary>
internal class BsonMinValue : BsonValue
{
    public override BsonType Type => BsonType.MinValue;

    public override int GetBytesCount() => 0;

    public override int GetHashCode() => this.Type.GetHashCode();

    #region Implement CompareTo

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other is BsonMinValue) return 0;

        return -1; // all types are heigher than MinValue
    }

    #endregion
}
