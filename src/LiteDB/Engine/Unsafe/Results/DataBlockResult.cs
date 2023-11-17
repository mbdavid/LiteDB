namespace LiteDB.Engine;

unsafe internal struct DataBlockResult : IIsEmpty
{
    public RowID DataBlockID;

    public PageMemoryResult Page;

    public PageSegment* Segment;
    public DataBlock* DataBlock;

    public static DataBlockResult Empty = new();

    public bool IsEmpty => this.DataBlockID.IsEmpty;

    /// <summary>
    /// Get current content length for this data block (discard header/padding)
    /// </summary>
    public int ContentLength => this.Segment->Length - sizeof(DataBlock) - this.DataBlock->Padding;

    /// <summary>
    /// Get full document size (only in first data block)
    /// </summary>
    public int DocumentLength => this.DataBlock->Extend ? -1 : 
        *(int*)(this.Page.Ptr + this.Segment->Location + sizeof(DataBlock)); // read first 4 bytes on datablock as int32 in first page only

    #region Shortcuts

    /// <summary>
    /// Shortcut for get/set DataBlock->NextBlockID (safe)
    /// </summary>
    public RowID NextBlockID { get => this.DataBlock->NextBlockID; set => this.DataBlock->NextBlockID = value; }

    #endregion

    public DataBlockResult()
    {
        this.DataBlockID = RowID.Empty;
        this.Page = PageMemoryResult.Empty;
    }

    public DataBlockResult(PageMemoryResult page, RowID dataBlockID)
    {
        ENSURE(page.PageID == dataBlockID.PageID);

        this.Page = page;
        this.DataBlockID = dataBlockID;

        this.Reload();
    }

    public void Reload()
    {
        this.Segment = PageMemory.GetSegmentPtr(this.Page.Page, this.DataBlockID.Index);
        this.DataBlock = (DataBlock*)(this.Page.Ptr + this.Segment->Location);
    }

    /// <summary>
    /// Get DataContent as Span
    /// </summary>
    public Span<byte> AsSpan()
    {
        return new Span<byte>((byte*)(this.Page.Ptr + this.Segment->Location + sizeof(DataBlock)), this.ContentLength);
    }
}
