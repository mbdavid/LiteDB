namespace LiteDB.Engine;

unsafe internal struct IndexNode   // 24
{
    public byte Slot;              // 1
    public byte Levels;            // 1
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public ushort Reserved1;       // 2
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public uint Reserved2;         // 4

    public RowID DataBlockID;      // 8
    public RowID NextNodeID;       // 8


    #region Static Helpers

    /// <summary>
    /// Calculate how many bytes this node will need on page block
    /// </summary>
    public unsafe static int GetSize(int levels, BsonValue key)
    {
        return 
            sizeof(IndexNode) +
            (levels * sizeof(IndexNodeLevel)) + // prev/next
            IndexKey.GetSize(key, out _);
    }

    #endregion
}
