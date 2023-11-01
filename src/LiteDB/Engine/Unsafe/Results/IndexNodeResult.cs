namespace LiteDB.Engine;

unsafe internal struct IndexNodeResult : IIsEmpty
{
    public RowID IndexNodeID;

    public PageMemory* Page;
    public PageSegment* Segment;
    public IndexNode* Node;
    public IndexKey* Key;

    /// <summary>
    /// Shortcut for get Node->DataBlockID (safe)
    /// </summary>
    public RowID DataBlockID => this.Node->DataBlockID;

    /// <summary>
    /// Shortcut for get/set Node->NextNodeID (safe) MUST set page as dirty in set!!
    /// </summary>
    public RowID NextNodeID { get => this.Node->NextNodeID; set => this.Node->NextNodeID = value; } 

    public static IndexNodeResult Empty = new() { IndexNodeID = RowID.Empty, Page = default };

    public bool IsEmpty => this.IndexNodeID.IsEmpty;

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

    public IndexNodeLevel* this[int level]
    {
        get
        {
            ENSURE(level <= this.Node->Levels);

            var ptr = ((nint)this.Node + sizeof(IndexNode) + (level * sizeof(IndexNodeLevel)));

            return (IndexNodeLevel*)ptr;
        }
    }

    public override string ToString()
    {
        return Dump.Object( new { IndexNodeID, DataBlockID = Node->DataBlockID, KeyType = Key->Type, KeyValue = IndexKey.ToBsonValue(Key) });
    }
}
