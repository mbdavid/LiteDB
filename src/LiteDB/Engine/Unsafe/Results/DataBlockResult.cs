namespace LiteDB.Engine;

unsafe internal struct DataBlockResult : IIsEmpty
{
    public RowID DataBlockID;

    public PageMemory* Page;
    public PageSegment* Segment;
    public DataBlock* DataBlock;

    public static DataBlockResult Empty = new() { DataBlockID = RowID.Empty, Page = default };

    public bool IsEmpty => this.DataBlockID.IsEmpty;

    /// <summary>
    /// Get current content length for this data block (discard header/padding)
    /// </summary>
    public int ContentLength => this.Segment->Length - sizeof(DataBlock) - this.DataBlock->Padding;

    /// <summary>
    /// Get full document size (only in first data block)
    /// </summary>
    public int DocumentLength => this.DataBlock->Extend ? -1 : 
        *(int*)((nint)this.Page + this.Segment->Location + sizeof(DataBlock)); // read first 4 bytes on datablock as int32 in first page only

    public DataBlockResult(nint ptr, RowID dataBlockID)
        : this((PageMemory*)ptr, dataBlockID)
    {
    }

    public DataBlockResult(PageMemory* page, RowID dataBlockID)
    {
        ENSURE(page->PageID == dataBlockID.PageID);

        this.Page = page;
        this.DataBlockID = dataBlockID;

        this.Reload();
    }

    public void Reload()
    {
        this.Segment = PageMemory.GetSegmentPtr(this.Page, this.DataBlockID.Index);
        this.DataBlock = (DataBlock*)((nint)this.Page + this.Segment->Location);
    }

    /// <summary>
    /// Get DataContent as Span
    /// </summary>
    public Span<byte> AsSpan()
    {
        return new Span<byte>((byte*)((nint)this.Page + this.Segment->Location + sizeof(DataBlock)), this.ContentLength);
    }

}
