namespace LiteDB.Engine;

internal struct RowID : IEquatable<RowID>, IIsEmpty
{
    public uint PageID;     // 4
    public ushort Index;    // 2
    public ushort Reserved; // 2

    public static readonly RowID Empty = new () { PageID = uint.MaxValue, Index = ushort.MaxValue };

    public bool IsEmpty => this.PageID == uint.MaxValue && this.Index == ushort.MaxValue;

    public RowID()
    {
    }

    public RowID(uint pageID, ushort index)
    {
        this.PageID = pageID;
        this.Index = index;
    }

    public bool Equals(RowID other)
    {
        return this.PageID == other.PageID && this.Index == other.Index;
    }
    public override bool Equals(object? other) => this.Equals((RowID)other!);

    public static bool operator ==(RowID left, RowID right)
    {
        return left.PageID == right.PageID && left.Index == right.Index;
    }

    public static bool operator !=(RowID left, RowID right)
    {
        return !(left == right);
    }

    public override int GetHashCode() => HashCode.Combine(PageID, Index);

    public override string ToString()
    {
        return IsEmpty ? "<EMPTY>" : $"{PageID:0000}:{Index:00}";
    }
}
