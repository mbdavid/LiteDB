namespace LiteDB.Engine;

unsafe internal partial struct PageMemory // PageMemory.IndexNode
{
    public static void InitializeAsIndexPage(PageMemory* page, uint pageID, byte colID)
    {
        page->PageID = pageID;
        page->PageType = PageType.Index;
        page->ColID = colID;

        page->IsDirty = true;
    }

    public static IndexNodeResult InsertIndexNode(PageMemory* page, byte slot, byte levels, BsonValue key, RowID dataBlockID, out bool defrag, out ExtendPageValue newPageValue)
    {
        // get a new index block
        var newIndex = PageMemory.GetFreeIndex(page);

        // get new rowid
        var indexNodeID = new RowID(page->PageID, newIndex);

        var nodeSize = IndexNode.GetSize(levels, key);

        // get page segment for this indexNode
        var segment = PageMemory.InsertSegment(page, (ushort)nodeSize, newIndex, true, out defrag, out newPageValue);

        var indexNode = (IndexNode*)((nint)page + segment->Location);

        // initialize indexNode
        indexNode->Slot = slot;
        indexNode->Levels = levels;
        indexNode->Reserved1 = 0;
        indexNode->Reserved2 = 0;
        indexNode->DataBlockID = dataBlockID;
        indexNode->NextNodeID = RowID.Empty;

        var levelPtr = (IndexNodeLevel*)((nint)indexNode + sizeof(IndexNode));

        for (var l = 0; l < levels; l++)
        {
            levelPtr->NextID = levelPtr->PrevID = RowID.Empty;
            levelPtr++;
        }

        // after write all level nodes, levelPtr are at IndexKey location
        var indexKey = (IndexKey*)levelPtr;

        // get new indexKey and copy to memory
        IndexKey.Initialize(indexKey, key);

        return new IndexNodeResult(page, indexNodeID);
    }
}

