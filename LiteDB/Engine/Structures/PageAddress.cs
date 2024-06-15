namespace LiteDB.Engine;

using System.Diagnostics;

/// <summary>
///     Represents a page address inside a page structure - index could be byte offset position OR index in a list (6
///     bytes)
/// </summary>
[DebuggerStepThrough]
internal struct PageAddress
{
    public const int SIZE = 5;

    public static PageAddress Empty = new PageAddress(uint.MaxValue, byte.MaxValue);

    /// <summary>
    ///     PageID (4 bytes)
    /// </summary>
    public readonly uint PageID;

    /// <summary>
    ///     Page Segment index inside page (1 bytes)
    /// </summary>
    public readonly byte Index;

    /// <summary>
    ///     Returns true if this PageAdress is empty value
    /// </summary>
    public bool IsEmpty => PageID == uint.MaxValue && Index == byte.MaxValue;

    public override bool Equals(object obj)
    {
        var other = (PageAddress) obj;

        return PageID == other.PageID && Index == other.Index;
    }

    public static bool operator ==(PageAddress lhs, PageAddress rhs)
    {
        return lhs.PageID == rhs.PageID && lhs.Index == rhs.Index;
    }

    public static bool operator !=(PageAddress lhs, PageAddress rhs)
    {
        return !(lhs == rhs);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + (int) PageID;
            hash = hash * 23 + Index;
            return hash;
        }
    }

    public PageAddress(uint pageID, byte index)
    {
        PageID = pageID;
        Index = index;
    }

    public override string ToString()
    {
        return IsEmpty ? "(empty)" : PageID.ToString().PadLeft(4, '0') + ":" + Index.ToString().PadLeft(2, '0');
    }

    public BsonValue ToBsonValue()
    {
        if (IsEmpty)
            return BsonValue.Null;

        return new BsonDocument
        {
            ["pageID"] = (int) PageID,
            ["index"] = (int) Index
        };
    }
}