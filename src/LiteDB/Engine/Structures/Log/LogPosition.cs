namespace LiteDB.Engine;

internal struct LogPosition : IEqualityComparer<LogPosition>
{
    public uint PositionID;
    public uint PageID;
    public uint PhysicalID;
    public bool IsConfirmed;

    public bool Equals(LogPosition x, LogPosition y)
    {
        return x.PositionID == y.PositionID;
    }

    public int GetHashCode(LogPosition obj) => HashCode.Combine(PositionID, PageID, PhysicalID, IsConfirmed);

    public override string ToString()
    {
        return Dump.Object(new { PhysicalID = Dump.PageID(PhysicalID), PositionID = Dump.PageID(PositionID), PageID = Dump.PageID(PageID), IsConfirmed });
    }
}
