namespace LiteDB;

/// <summary>
/// Represent a Binary value (byte array) in Bson object model
/// </summary>
public class BsonBinary : BsonValue
{
    public byte[] Value { get; }

    public BsonBinary(byte[] value)
    {
        this.Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override BsonType Type => BsonType.Binary;

    public override int GetBytesCount() => this.Value.Length;

    public override int GetHashCode() => this.Value.GetHashCode();

    #region Implement CompareTo

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other is BsonBinary otherBinary) return this.Value.AsSpan().SequenceCompareTo(otherBinary.Value);

        return this.CompareType(other);
    }

    #endregion
}
