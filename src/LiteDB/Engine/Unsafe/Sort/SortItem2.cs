namespace LiteDB.Engine;

internal struct SortItem2      // 16 + (n * 8)
{
    public RowID DataBlockID;  // 8
    public IndexKey Key;       // 8 + (n * 8)
}
