namespace LiteDB.Engine;

internal struct LogPageHeader
{
    public uint PositionID;
    public uint PageID;
    public int TransactionID;
    public bool IsConfirmed;

    public LogPageHeader()
    {
    }

    public unsafe LogPageHeader(PageMemory* page)
    {
        this.PositionID = page->PositionID;
        this.PageID = page->PageID;
        this.TransactionID = page->TransactionID;
        this.IsConfirmed = page->IsConfirmed;
    }
}
