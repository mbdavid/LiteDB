namespace LiteDB.Engine;

[AutoInterface]
unsafe internal partial struct PageMemory // PageMemory.DataBlock
{
    public static void InitializeAsDataPage(nint ptr, uint pageID, byte colID)
    {
        // cast pointer type as PageMemory pointer
        var page = (PageMemory*)ptr;

        page->PageID = pageID;
        page->PageType = PageType.Data;
        page->ColID = colID;

        page->IsDirty = true;
    }

    public static DataBlockResult InsertDataBlock(PageMemoryResult pageResult, Span<byte> content, bool extend, out bool defrag, out ExtendPageValue newPageValue)
    {
        // get PageMemory
        var page = pageResult.Page;

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

        var result = new DataBlockResult(pageResult, dataBlockID);

        // copy content into dataBlock content block
        content.CopyTo(result.AsSpan());

        return result;
    }


    /// <summary>
    /// Update an existing document inside a single page. This new document must fit on this page
    /// </summary>
    public static void UpdateDataBlock(PageMemoryResult pageResult, ushort index, Span<byte> content, RowID nextBlock, out bool defrag, out ExtendPageValue newPageValue)
    {
        // get PageMemory
        var page = pageResult.Page;

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

    /// <summary>
    /// Get how many bytes a page contains to be used in a new DataBlock content size (should consider new block fix size)
    /// </summary>
    public static int GetPageAvailableSpace(nint ptr)
    {
        var page = (PageMemory*)ptr;

        return page->FreeBytes -
               sizeof(DataBlock) - // new data block fixed syze
               (sizeof(PageSegment) * 2) - // footer (*2 to align)
               8; // extra align
    }
}
