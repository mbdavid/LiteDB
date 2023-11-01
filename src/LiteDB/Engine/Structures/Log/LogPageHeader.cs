namespace LiteDB.Engine;

internal struct LogPageHeader
{
    public uint PositionID;
    public uint PageID;
    public int TransactionID;
    public bool IsConfirmed;
}
