namespace LiteDB.Engine;

unsafe internal struct IndexNodeResult : IIsEmpty
{
    public RowID IndexNodeID;

    public PageMemory* Page;
    public PageSegment* Segment;
    public IndexNode* Node;
    public IndexKey* Key;

    #region Shortcuts

    /// <summary>
    /// Shortcut for get Page->PageID
    /// </summary>
    public uint PageID => this.Page->PageID;

    /// <summary>
    /// Shortcut for get Node->DataBlockID (safe)
    /// </summary>
    public RowID DataBlockID => this.Node->DataBlockID;

    /// <summary>
    /// Shortcut for get/set Node->NextNodeID (safe) MUST set page as dirty in set!!
    /// </summary>
    public RowID NextNodeID { get => this.Node->NextNodeID; set => this.Node->NextNodeID = value; }

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
    /// Shortcut for get/set current page as dirty Page->IsDirty
    /// </summary>
    public bool IsDirtyPage { get => this.Page->IsDirty; set => this.Page->IsDirty = value; }

    /// <summary>
    /// Shortcut for get if Key-Type is MinValue or MaxValue
    /// </summary>
    public bool IsMinOrMaxValue => this.Key->IsMinValue || this.Key->IsMaxValue;

    /// <summary>
    /// Shortcut for IndexKey.CompareTo(value)
    /// </summary>
    public int KeyCompareTo(BsonValue value, Collation collation) => IndexKey.Compare(this.Key, value, collation);

    #endregion

    public static IndexNodeResult Empty = new() { IndexNodeID = RowID.Empty, Page = default };

    public bool IsEmpty => this.IndexNodeID.IsEmpty;

    public IndexNodeResult(nint ptr, RowID indexNodeID)
        : this((PageMemory*)ptr, indexNodeID)
    {
    }

    public IndexNodeResult(PageMemory* page, RowID indexNodeID)
    {
        ENSURE(page->PageID == indexNodeID.PageID);

        this.Page = page;
        this.IndexNodeID = indexNodeID;

        this.Reload();

        ENSURE(this.Segment->AsSpan(page).IsFullZero() == false);
        ENSURE(this.Node->Levels > 0 && this.Node->Levels <= INDEX_MAX_LEVELS);
    }

    public void Reload()
    {
        // load all pointer based on indexNodeID and &page
        this.Segment = PageMemory.GetSegmentPtr(this.Page, this.IndexNodeID.Index);
        this.Node = (IndexNode*)((nint)this.Page + this.Segment->Location);
        var keyOffset = this.Segment->Location + sizeof(IndexNode) + (this.Node->Levels * sizeof(IndexNodeLevel));
        this.Key = (IndexKey*)((nint)this.Page + keyOffset);
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
