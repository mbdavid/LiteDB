namespace LiteDB;

/// <summary>
/// Represent an ObjectId value (12 bytes sequencial guid-like) in Bson object model
/// </summary>
internal class BsonObjectId : BsonValue
{
    public static BsonObjectId Empty = new (ObjectId.Empty);

    public ObjectId Value { get; }

    public BsonObjectId(ObjectId value)
    {
        this.Value = value;
    }

    public override BsonType Type => BsonType.ObjectId;

    public override int GetBytesCount() => 12;

    public override int GetHashCode() => this.Value.GetHashCode();

    #region Implement CompareTo

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other is BsonObjectId otherObjectId) return this.Value.CompareTo(otherObjectId.Value);

        return this.CompareType(other);
    }

    #endregion

    #region Implicit Ctor

    public static implicit operator ObjectId(BsonObjectId value) => value.AsObjectId;

    public static implicit operator BsonObjectId(ObjectId value) => value == ObjectId.Empty ? Empty : new BsonObjectId(value);

    #endregion
}
