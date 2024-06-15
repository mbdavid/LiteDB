namespace LiteDB.Engine;

using System.Collections.Generic;

/// <summary>
///     The IndexPage thats stores object data.
/// </summary>
internal class IndexPage : BasePage
{
    /// <summary>
    ///     Read existing IndexPage in buffer
    /// </summary>
    public IndexPage(PageBuffer buffer)
        : base(buffer)
    {
        ENSURE(PageType == PageType.Index, "page type must be index page");

        if (PageType != PageType.Index)
            throw LiteException.InvalidPageType(PageType.Index, this);
    }

    /// <summary>
    ///     Create new IndexPage
    /// </summary>
    public IndexPage(PageBuffer buffer, uint pageID)
        : base(buffer, pageID, PageType.Index)
    {
    }

    /// <summary>
    ///     Read single IndexNode
    /// </summary>
    public IndexNode GetIndexNode(byte index)
    {
        var segment = Get(index);

        var node = new IndexNode(this, index, segment);

        return node;
    }

    /// <summary>
    ///     Insert new IndexNode. After call this, "node" instance can't be changed
    /// </summary>
    public IndexNode InsertIndexNode(byte slot, byte level, BsonValue key, PageAddress dataBlock, int bytesLength)
    {
        var segment = Insert((ushort) bytesLength, out var index);

        var node = new IndexNode(this, index, segment, slot, level, key, dataBlock);

        return node;
    }

    /// <summary>
    ///     Delete index node based on page index
    /// </summary>
    public void DeleteIndexNode(byte index)
    {
        Delete(index);
    }

    /// <summary>
    ///     Get all index nodes inside this page
    /// </summary>
    public IEnumerable<IndexNode> GetIndexNodes()
    {
        foreach (var index in GetUsedIndexs())
        {
            yield return GetIndexNode(index);
        }
    }

    /// <summary>
    ///     Get page index slot on FreeIndexPageID
    ///     8160 - 600 : Slot #0
    ///     599  -   0 : Slot #1 (no page in list)
    /// </summary>
    public static byte FreeIndexSlot(int freeBytes)
    {
        ENSURE(freeBytes >= 0, "freeBytes must be positive");

        return freeBytes >= MAX_INDEX_LENGTH ? (byte) 0 : (byte) 1;
    }
}