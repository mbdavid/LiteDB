namespace LiteDB.Engine;

[AutoInterface]
unsafe internal partial struct PageMemory // PageMemory.DataBlock
{
    public static void InitializeAsDataPage(PageMemory* page, uint pageID, byte colID)
    {
        page->PageID = pageID;
        page->PageType = PageType.Data;
        page->ColID = colID;

        page->IsDirty = true;
    }

    public static DataBlockResult InsertDataBlock(PageMemory* page, Span<byte> content, bool extend, out bool defrag, out ExtendPageValue newPageValue)
    {
        // get required bytes this insert
        var bytesLength = sizeof(DataBlock) + content.Length;

        // get padding for dataBlock fit in % 8
        var padding = bytesLength % 8 > 0 ? 8 - (bytesLength % 8) : 0;

        bytesLength += padding;

        // get a new index block
        var newIndex = PageMemory.GetFreeIndex(page);

        // get new rowid
        var dataBlockID = new RowID(page->PageID, newIndex);

        // get page segment for this data block
        var segment = PageMemory.InsertSegment(page, (ushort)bytesLength, newIndex, true, out defrag, out newPageValue);

        var dataBlock = (DataBlock*)((nint)page + segment->Location);

        // initialize dataBlock
        dataBlock->DataFormat = 0; // Bson
        dataBlock->Extend = extend;
        dataBlock->Padding = (byte)padding;
        dataBlock->NextBlockID = RowID.Empty;

        var result = new DataBlockResult(page, dataBlockID);

        // copy content into dataBlock content block
        content.CopyTo(result.AsSpan());

        return result;
    }


    /// <summary>
    /// Update an existing document inside a single page. This new document must fit on this page
    /// </summary>
    public static void UpdateDataBlock(PageMemory* page, ushort index, Span<byte> content, RowID nextBlock, out bool defrag, out ExtendPageValue newPageValue)
    {
        // get required bytes this insert
        var bytesLength = sizeof(DataBlock) + content.Length;

        // get padding for dataBlock fit in % 8
        var padding = bytesLength % 8 > 0 ? 8 - (bytesLength % 8) : 0;

        bytesLength += padding;

        page->IsDirty = true;

        // get page segment to update this buffer
        var segment = PageMemory.UpdateSegment(page, index, (ushort)bytesLength, out defrag, out newPageValue);

        // get dataBlock pointer
        var dataBlock = (DataBlock*)((nint)page + segment->Location);

        dataBlock->Padding = (byte)padding;
        dataBlock->DataFormat = 0; // Bson
        dataBlock->NextBlockID = nextBlock;

        // get datablock content pointer
        var contentPtr = (byte*)((nint)page + segment->Location + sizeof(DataBlock));

        // create a span and copy from source
        var dataBlockSpan = new Span<byte>(contentPtr, bytesLength);

        content.CopyTo(dataBlockSpan);
    }
}
