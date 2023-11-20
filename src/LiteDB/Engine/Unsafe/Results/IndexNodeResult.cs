namespace LiteDB.Engine;

unsafe internal struct IndexNodeResult : IIsEmpty
{
    public readonly RowID IndexNodeID;

    public readonly PageMemoryResult Page;

    public PageSegment* Segment;
    public IndexNode* Node;
    public IndexKey* Key;

    #region Shortcuts

    /// <summary>
    /// Shortcut for get Page->PageID
    /// </summary>
    public uint PageID => this.Page.Page->PageID;

    /// <summary>
    /// Shortcut for get Node->DataBlockID (safe)
    /// </summary>
    public RowID DataBlockID => this.Node->DataBlockID;

    /// <summary>
    /// Shortcut for get/set Node->NextNodeID (safe) MUST set page as dirty in set!!
    /// </summary>
    public RowID NextNodeID { get => this.Node->NextNodeID; set => this.Node->NextNodeID = value; }

    /// <summary>
    /// Shortcut for get Node->Slot
    /// </summary>
    public byte Slot => this.Node->Slot;

    /// <summary>
    /// Shortcut for get Node->Levels
    /// </summary>
    public byte Levels => this.Node->Levels;

    /// <summary>
    /// Shortcut for get Node->NextID (or PrevID) according level and order
    /// </summary>
    public RowID GetNextID(int level, int order = Query.Ascending) => this.GetLevel(level)->GetNext(order);

    /// <summary>
    /// Shortcut for set Node->NextID according level and order
    /// </summary>
    public RowID SetNextID(int level, RowID value) => this.GetLevel(level)->NextID = value;

    /// <summary>
    /// Shortcut for get Node->PrevID (or NextID) according level
    /// </summary>
    public RowID GetPrevID(int level, int order = Query.Ascending) => this.GetLevel(level)->GetPrev(order);

    /// <summary>
    /// Shortcut for set Node->PrevID according level
    /// </summary>
    public RowID SetPrevID(int level, RowID value) => this.GetLevel(level)->PrevID = value;

    /// <summary>
    /// Shortcut for get if Key-Type is MinValue or MaxValue
    /// </summary>
    public bool IsMinOrMaxValue => this.Key->IsMinValue || this.Key->IsMaxValue;

    /// <summary>
    /// Shortcut for get if Key->Type == String
    /// </summary>
    public bool IsStringValue => this.Key->Type == BsonType.String;

    /// <summary>
    /// Shortcut for IndexKey.ToBsonValue(value)
    /// </summary>
    public BsonValue ToBsonValue() => IndexKey.ToBsonValue(this.Key);

    #endregion

    public static IndexNodeResult Empty = new();

    public bool IsEmpty => this.IndexNodeID.IsEmpty;

    public IndexNodeResult(PageMemoryResult page, RowID indexNodeID)
    {
        ENSURE(page.PageID == indexNodeID.PageID);

        this.Page = page;
        this.IndexNodeID = indexNodeID;

        this.Reload();

        ENSURE(this.Segment->AsSpan(page.Page).IsFullZero() == false);
        ENSURE(this.Node->Levels > 0 && this.Node->Levels <= INDEX_MAX_LEVELS);
    }

    public IndexNodeResult()
    {
        this.IndexNodeID = RowID.Empty;
        this.Page = PageMemoryResult.Empty;
    }

    public void Reload()
    {
        // load all pointer based on indexNodeID and &page
        this.Segment = PageMemory.GetSegmentPtr(this.Page.Page, this.IndexNodeID.Index);
        this.Node = (IndexNode*)(this.Page.Ptr + this.Segment->Location);
        var keyOffset = this.Segment->Location + sizeof(IndexNode) + (this.Node->Levels * sizeof(IndexNodeLevel));
        this.Key = (IndexKey*)(this.Page.Ptr + keyOffset);
    }

    private IndexNodeLevel* GetLevel(int level)
    {
        ENSURE(level <= this.Node->Levels);

        var ptr = ((nint)this.Node + sizeof(IndexNode) + (level * sizeof(IndexNodeLevel)));

        return (IndexNodeLevel*)ptr;
    }

    public override string ToString()
    {
        return Dump.Object( new { IndexNodeID, DataBlockID = Node->DataBlockID, KeyType = Key->Type, KeyValue = IndexKey.ToBsonValue(Key) });
    }
}
